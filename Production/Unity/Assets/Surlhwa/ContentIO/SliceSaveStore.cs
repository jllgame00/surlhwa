using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Surlhwa.Application;
using Surlhwa.Domain;

namespace Surlhwa.ContentIO
{
    public sealed class SliceLoadResult
    {
        public SliceCheckpoint Checkpoint { get; } public bool RecoveryRequired { get; } public string Message { get; }
        internal SliceLoadResult(SliceCheckpoint state,bool recovery,string message) { Checkpoint=state; RecoveryRequired=recovery; Message=message; }
    }
    public sealed class SliceSaveStore : ISliceSaveStore
    {
        private readonly string folder; private readonly SliceContent content; private readonly SemaphoreSlim writer=new SemaphoreSlim(1,1);
        public string CurrentPath => Path.Combine(folder,"current.json"); public string PreviousPath => Path.Combine(folder,"previous.json");
        public Action<string> Fault { get; set; } public double LastWriteMilliseconds { get; private set; }
        public IReadOnlyDictionary<string,double> LastWriteStages { get; private set; }=new ReadOnlyDictionary<string,double>(new Dictionary<string,double>());
        public SliceSaveStore(string slotFolder,SliceContent content) { folder=Path.GetFullPath(slotFolder); this.content=content; }
        public SliceLoadResult Load()
        {
            string currentError=null;
            if(File.Exists(CurrentPath))
            {
                try { return new SliceLoadResult(Read(CurrentPath),false,null); }
                catch(ContentValidationException e) { currentError=e.Message; }
            }
            if(File.Exists(PreviousPath)) return new SliceLoadResult(Read(PreviousPath),true,currentError ?? "Current generation missing; previous generation available.");
            if(currentError!=null) throw new ContentValidationException("No valid save generation: "+currentError);
            return new SliceLoadResult(null,false,null);
        }
        private SliceCheckpoint Read(string path)
        { if(new FileInfo(path).Length>4000000) throw new ContentValidationException("Oversized save."); return SliceSaveCodec.Decode(File.ReadAllText(path,Encoding.UTF8),content); }
        public async Task SaveAsync(SliceCheckpoint checkpoint)
        {
            await writer.WaitAsync().ConfigureAwait(false);
            try { await Task.Run(()=>Write(checkpoint)).ConfigureAwait(false); }
            finally { writer.Release(); }
        }
        private void Write(SliceCheckpoint checkpoint)
        {
            var timer=Stopwatch.StartNew(); var stages=new Dictionary<string,double>(); double prior=0;
            void Mark(string name) { double now=timer.Elapsed.TotalMilliseconds; stages[name]=now-prior; prior=now; }
            string json=SliceSaveCodec.Encode(checkpoint,content); Directory.CreateDirectory(folder); Mark("encode");
            bool validCurrent=false;
            if(File.Exists(CurrentPath))
            {
                try
                {
                    var existing=Read(CurrentPath); validCurrent=true;
                    if(existing.RunId==checkpoint.RunId && existing.Sequence>=checkpoint.Sequence)
                    {
                        if(existing.Sequence==checkpoint.Sequence && File.ReadAllText(CurrentPath,Encoding.UTF8)==json) return;
                        throw new InvalidOperationException("Refusing stale or conflicting save transaction.");
                    }
                }
                catch(ContentValidationException) { /* Preserve corrupt bytes before publishing recovery. */ }
            }
            string temp=Path.Combine(folder,"pending-"+Guid.NewGuid().ToString("N")+".json");
            Mark("read_current");
            try
            {
                Fault?.Invoke("before_write"); byte[] bytes=new UTF8Encoding(false).GetBytes(json);
                using(var file=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None))
                { file.Write(bytes,0,bytes.Length); Fault?.Invoke("after_write"); Mark("write"); file.Flush(true); Fault?.Invoke("after_flush"); Mark("flush"); }
                Read(temp); Fault?.Invoke("after_validate"); Mark("validate_temp");
                if(File.Exists(CurrentPath))
                {
                    if(!validCurrent) File.Copy(CurrentPath,Path.Combine(folder,"corrupt-"+Guid.NewGuid().ToString("N")+".json"));
                    File.Replace(temp,CurrentPath,validCurrent ? PreviousPath : null);
                }
                else File.Move(temp,CurrentPath);
                Fault?.Invoke("after_replace"); Mark("publish");
                var final=Read(CurrentPath); if(final.RunId!=checkpoint.RunId || final.Sequence!=checkpoint.Sequence) throw new IOException("Published checkpoint identity mismatch.");
                Fault?.Invoke("after_final_read"); Mark("validate_final");
            }
            finally
            {
                // Only this operation's exact temporary file is eligible for cleanup.
                if(File.Exists(temp)) File.Delete(temp); LastWriteMilliseconds=timer.Elapsed.TotalMilliseconds; LastWriteStages=new ReadOnlyDictionary<string,double>(stages);
            }
        }
    }
}
