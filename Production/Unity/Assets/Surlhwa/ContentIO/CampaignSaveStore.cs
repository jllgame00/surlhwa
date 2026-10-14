using System.IO;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Surlhwa.Domain;
using Surlhwa.Application;
namespace Surlhwa.ContentIO
{
    public sealed class CampaignLoadResult
    {
        public CampaignCheckpoint Checkpoint { get; } public bool RecoveryRequired { get; } public string Message { get; }
        public CampaignLoadResult(CampaignCheckpoint state,bool recovery,string message) { Checkpoint=state; RecoveryRequired=recovery; Message=message; }
    }
    public sealed class CampaignSaveStore : ICampaignSaveStore
    {
        private readonly string folder; private readonly CampaignContent content; private readonly SemaphoreSlim writer=new SemaphoreSlim(1,1);
        public string CurrentPath => Path.Combine(folder,"current.json"); public string PreviousPath => Path.Combine(folder,"previous.json");
        public Action<string> Fault { get; set; }
        public CampaignSaveStore(string folder,CampaignContent content) { this.folder=Path.GetFullPath(folder); this.content=content; }
        private CampaignCheckpoint Read(string path)
        { if(new FileInfo(path).Length>4000000) throw new ContentValidationException("Oversized campaign save."); return CampaignSaveCodec.Decode(File.ReadAllText(path,Encoding.UTF8),content); }
        public CampaignLoadResult Load()
        {
            string failure=null;
            if(File.Exists(CurrentPath))
            {
                try { return new CampaignLoadResult(Read(CurrentPath),false,null); }
                catch(ContentValidationException e) { failure=e.Message; }
            }
            if(File.Exists(PreviousPath)) return new CampaignLoadResult(Read(PreviousPath),true,failure ?? "Current campaign generation missing.");
            if(failure!=null) throw new ContentValidationException("No valid campaign generation: "+failure);
            return new CampaignLoadResult(null,false,null);
        }
        public async Task SaveAsync(CampaignCheckpoint state)
        {
            await writer.WaitAsync().ConfigureAwait(false);
            try { await Task.Run(()=>Write(state)).ConfigureAwait(false); } finally { writer.Release(); }
        }
        private void Write(CampaignCheckpoint state)
        {
            string json=CampaignSaveCodec.Encode(state,content); Directory.CreateDirectory(folder); bool valid=false;
            if(File.Exists(CurrentPath))
            {
                try
                {
                    var prior=Read(CurrentPath); valid=true;
                    if(prior.RunId==state.RunId && prior.Sequence>=state.Sequence)
                    {
                        if(prior.Sequence==state.Sequence && File.ReadAllText(CurrentPath,Encoding.UTF8)==json) return;
                        throw new InvalidOperationException("Refusing stale/conflicting campaign publication.");
                    }
                }
                catch(ContentValidationException) { }
            }
            string temp=Path.Combine(folder,"pending-"+Guid.NewGuid().ToString("N")+".json");
            try
            {
                Fault?.Invoke("before_write"); byte[] bytes=new UTF8Encoding(false).GetBytes(json);
                using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None))
                { stream.Write(bytes,0,bytes.Length); Fault?.Invoke("after_write"); stream.Flush(true); Fault?.Invoke("after_flush"); }
                Read(temp); Fault?.Invoke("after_validate");
                if(File.Exists(CurrentPath))
                {
                    if(!valid) File.Copy(CurrentPath,Path.Combine(folder,"corrupt-"+Guid.NewGuid().ToString("N")+".json"));
                    File.Replace(temp,CurrentPath,valid ? PreviousPath : null);
                }
                else File.Move(temp,CurrentPath);
                Fault?.Invoke("after_replace"); var final=Read(CurrentPath);
                if(final.RunId!=state.RunId || final.Sequence!=state.Sequence) throw new IOException("Published campaign identity mismatch.");
                Fault?.Invoke("after_final_read");
            }
            finally { if(File.Exists(temp)) File.Delete(temp); }
        }
    }
}
