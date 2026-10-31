using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Surlhwa.Domain;
namespace Surlhwa.Presentation
{
    public sealed class CampaignUi : MonoBehaviour
    {
        private CampaignController owner; private TMP_FontAsset font; private RectTransform root;
        private GameObject welcome,dialogue,brewing,ingredients,modal,requestPanel,choicePanel;
        private TextMeshProUGUI speaker,body,request,measurement,status,title;
        private Button next,continueButton; private readonly Stack<GameObject> focus=new Stack<GameObject>();
        private readonly List<GameObject> methodPanels=new List<GameObject>(); private int method,recipePage;
        private readonly List<GameObject> methodMarks=new List<GameObject>();
        private GameObject classicStir,expandedStir;
        private readonly Dictionary<string,Button> shelf=new Dictionary<string,Button>();
        private string shownModal; public string MenuModal; public string VisibleModal=>shownModal;
        public string DisplayedSpeaker=>speaker.text;
        public bool ModalOwnsFocus=>modal!=null && EventSystem.current?.currentSelectedGameObject!=null && EventSystem.current.currentSelectedGameObject.transform.IsChildOf(modal.transform);
        private readonly Color ink=new Color(.93f,.86f,.73f),muted=new Color(.70f,.68f,.62f),paper=new Color(.075f,.068f,.061f,.97f),gold=new Color(.72f,.53f,.29f);
        public void Initialize(CampaignController controller,TMP_FontAsset face)
        {
            owner=controller; font=face;
            var canvas=new GameObject("ACT 1 reading and tools",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            root=canvas.GetComponent<RectTransform>(); root.SetParent(transform,false);
            var surface=canvas.GetComponent<Canvas>(); surface.renderMode=RenderMode.ScreenSpaceCamera; surface.worldCamera=owner.Camera; surface.planeDistance=1; surface.sortingOrder=100;
            var scaler=canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1280,720); scaler.matchWidthOrHeight=.5f;
            var events=new GameObject("ACT 1 input focus",typeof(EventSystem),typeof(InputSystemUIInputModule)); events.transform.SetParent(transform);
            var module=events.GetComponent<InputSystemUIInputModule>(); module.AssignDefaultActions(); module.submit=null; module.cancel=null;
            Panel(root,new Rect(0,664,1280,56),paper); title=Text(root,new Rect(28,675,600,30),"설화   /   첫 번째 이야기",20,ink);
            Button(root,new Rect(913,673,102,34),"손길  B",()=>owner.OpenModal("guide"));
            Button(root,new Rect(1023,673,102,34),"도감  Tab",()=>owner.OpenModal("codex"));
            Button(root,new Rect(1133,673,119,34),"설정  Esc",()=>owner.OpenModal("settings"));
            status=Text(root,new Rect(28,627,550,28),"",16,muted);
            requestPanel=Panel(root,new Rect(292,582,696,66),paper); request=Text(requestPanel.transform,new Rect(16,5,664,56),"",18,ink); request.alignment=TextAlignmentOptions.Center;
            welcome=Panel(root,new Rect(348,194,584,318),paper);
            Text(welcome.transform,new Rect(36,222,512,60),"설화",40,ink);
            Text(welcome.transform,new Rect(36,108,512,104),"눈이 내리는 밤, 문을 열고.\n한 잔과 이야기를 기다립니다.",23,ink);
            var newGame=Button(welcome.transform,new Rect(36,34,242,48),"새로 시작",()=>owner.BeginNew());
            continueButton=Button(welcome.transform,new Rect(294,34,254,48),"이어 하기",()=>owner.ContinueRun());
            dialogue=Panel(root,new Rect(216,24,848,176),paper);
            speaker=Text(dialogue.transform,new Rect(26,132,796,28),"",17,muted);
            body=Text(dialogue.transform,new Rect(26,48,796,82),"",21,ink);
            next=Button(dialogue.transform,new Rect(660,8,162,34),"계속  Enter",()=>owner.Advance());
            choicePanel=Panel(root,new Rect(216,204,848,98),paper);
            int ci=0; foreach(var pair in owner.Content.PrinceChoices) { string key=pair.Key; Button(choicePanel.transform,new Rect(20+ci*410,16,398,66),owner.Content.Texts[pair.Value].Value,()=>owner.Choose(key),18); ci++; }
            brewing=Panel(root,new Rect(220,18,1032,174),paper);
            measurement=Text(brewing.transform,new Rect(18,134,996,28),"",16,ink);
            string[] methods={"Stir","Build","Shake","Float"};
            for(int n=0;n<4;n++) {
                int index=n; var control=Button(brewing.transform,new Rect(18+n*120,92,112,34),methods[n],()=>SelectMethod(index));
                methodMarks.Add(Rule(control.transform,new Rect(12,2,88,2),gold));
            }
            Button(brewing.transform,new Rect(508,92,120,34),"대상 변경 J",()=>owner.Input.Command("target"),15);
            Button(brewing.transform,new Rect(636,92,120,34),"미세 따르기",()=>owner.Input.Fine=!owner.Input.Fine,15);
            Button(brewing.transform,new Rect(764,92,112,34),"잔 바꾸기 V",()=>owner.HandleCommand("glass"),15);
            Button(brewing.transform,new Rect(884,92,130,34),"손님께 내기",()=>owner.Advance(),16);
            for(int n=0;n<4;n++) methodPanels.Add(Panel(brewing.transform,new Rect(12,8,1008,76),Color.clear));
            classicStir=Panel(methodPanels[0].transform,new Rect(0,0,1008,76),Color.clear);
            Hold(classicStir.transform,new Rect(6,29,140,38),"따르기 Q","pour"); Hold(classicStir.transform,new Rect(154,29,140,38),"지거 이동 E","transfer");
            Hold(classicStir.transform,new Rect(302,29,140,38),"원 그리기 S","stir"); Hold(classicStir.transform,new Rect(450,29,140,38),"거르기 R","strain");
            Cmd(classicStir.transform,598,"얼음 I","ice"); Cmd(classicStir.transform,730,"Dash D","dash"); Cmd(classicStir.transform,862,"체리 C","cherry");
            expandedStir=Panel(methodPanels[0].transform,new Rect(0,0,1008,76),Color.clear);
            Hold(expandedStir.transform,new Rect(6,29,116,38),"따르기 Q","pour"); Hold(expandedStir.transform,new Rect(130,29,116,38),"지거 이동 E","transfer");
            Hold(expandedStir.transform,new Rect(254,29,116,38),"원 그리기 S","stir"); Hold(expandedStir.transform,new Rect(378,29,116,38),"거르기 R","strain");
            string[] garnishLabels={"얼음 I","Dash D","체리 C","제스트 Z"},garnishCommands={"ice","dash","cherry","zest"};
            for(int n=0;n<4;n++) { string command=garnishCommands[n]; Button(expandedStir.transform,new Rect(502+n*124,29,116,38),garnishLabels[n],()=>owner.Input.Command(command),15); }
            Text(methodPanels[0].transform,new Rect(6,0,990,25),"S를 누르고 혼합 잔 둘레를 천천히 돕니다. 혼합 잔에 얼음이 필요합니다.",14,muted);
            Hold(methodPanels[1].transform,new Rect(6,29,174,38),"따르기 Q","pour"); Hold(methodPanels[1].transform,new Rect(188,29,174,38),"지거 이동 E","transfer");
            Cmd(methodPanels[1].transform,370,"얼음 I","ice"); Cmd(methodPanels[1].transform,502,"가볍게 젓기 G","gentle"); Cmd(methodPanels[1].transform,634,"오렌지 O","orange");
            Text(methodPanels[1].transform,new Rect(6,0,990,25),"서빙 잔에 직접 담고 가볍게 섞습니다. 지거를 쓸 때는 이동할 대상을 먼저 고릅니다.",14,muted);
            Hold(methodPanels[2].transform,new Rect(6,29,124,38),"따르기 Q","pour"); Cmd(methodPanels[2].transform,138,"얼음 I","ice"); Cmd(methodPanels[2].transform,270,"뚜껑 L","lid");
            Hold(methodPanels[2].transform,new Rect(402,29,144,38),"흔들기 K","shake"); Hold(methodPanels[2].transform,new Rect(554,29,144,38),"잔에 옮기기 T","shaker_transfer"); Hold(methodPanels[2].transform,new Rect(706,29,144,38),"Splash X","splash");
            Text(methodPanels[2].transform,new Rect(6,0,990,25),"뚜껑을 닫고 K를 누른 채 화면 중앙에서 좌우로 흔듭니다. 뚜껑을 열고 T로 잔에 옮깁니다.",14,muted);
            Text(methodPanels[2].transform,new Rect(862,29,124,38),"←  K  →",18,ink).alignment=TextAlignmentOptions.Center;
            Hold(methodPanels[3].transform,new Rect(6,29,174,38),"지거에 따르기 Q","pour"); Hold(methodPanels[3].transform,new Rect(188,29,190,38),"층 쌓기 F","float");
            Text(methodPanels[3].transform,new Rect(6,0,990,25),"지거로 양을 재고 F를 누릅니다. 낮게, 천천히 — 오른쪽은 유량, 위쪽은 높이입니다.",14,muted);
            Text(methodPanels[3].transform,new Rect(402,29,584,38),"F · 화면 중앙 아래에서 낮게, 천천히   |   높이 ↑   유량 →",16,ink).alignment=TextAlignmentOptions.Center;
            ingredients=Panel(root,new Rect(24,188,180,464),paper); Text(ingredients.transform,new Rect(12,430,156,28),"재료 선반",17,ink);
            int i=0; foreach(var id in owner.Content.Supply) { string ingredient=id; shelf[id]=Button(ingredients.transform,new Rect(10,402-i*25,160,24),(i<9 ? (i+1)+"  " : "")+owner.Content.IngredientNames[id],()=>owner.Input.SelectIngredient(ingredient),14); i++; }
            Button(ingredients.transform,new Rect(10,8,160,30),"현재 잔 비우기",()=>owner.DiscardDrink(),14);
            SelectMethod(0); Refresh(); EventSystem.current.SetSelectedGameObject(newGame.gameObject);
        }
        private void Cmd(Transform parent,float x,string label,string command)=>Button(parent,new Rect(x,29,124,38),label,()=>owner.Input.Command(command),15);
        private void SelectMethod(int index) { owner.Input.Stop(); method=index; for(int i=0;i<methodPanels.Count;i++) methodPanels[i].SetActive(i==index); for(int i=0;i<methodMarks.Count;i++) methodMarks[i].SetActive(i==index); }
        private GameObject Panel(Transform parent,Rect rect,Color color)
        {
            var g=new GameObject("Panel",typeof(RectTransform),typeof(Image)); g.transform.SetParent(parent,false); Position((RectTransform)g.transform,rect); g.GetComponent<Image>().color=color;
            if(rect.width>160 && rect.height>60 && color.a>.9f) {
                var edge=new Color(.40f,.31f,.21f,.8f);
                Rule(g.transform,new Rect(1,rect.height-2,rect.width-2,1),edge);
                Rule(g.transform,new Rect(1,1,rect.width-2,1),edge);
                Rule(g.transform,new Rect(1,1,1,rect.height-2),edge);
                Rule(g.transform,new Rect(rect.width-2,1,1,rect.height-2),edge);
                foreach(float x in new[]{5f,rect.width-18f}) Rule(g.transform,new Rect(x,rect.height-6,13,2),gold);
            }
            return g;
        }
        private static GameObject Rule(Transform parent,Rect rect,Color color)
        {
            var g=new GameObject("Quiet brass rule",typeof(RectTransform),typeof(Image)); g.transform.SetParent(parent,false);
            Position((RectTransform)g.transform,rect); var image=g.GetComponent<Image>(); image.color=color; image.raycastTarget=false; return g;
        }
        private static void Position(RectTransform t,Rect r) { t.anchorMin=t.anchorMax=Vector2.zero; t.pivot=Vector2.zero; t.anchoredPosition=r.position; t.sizeDelta=r.size; }
        private TextMeshProUGUI Text(Transform parent,Rect rect,string value,float size,Color color)
        {
            var g=new GameObject("Text",typeof(RectTransform),typeof(TextMeshProUGUI)); g.transform.SetParent(parent,false); Position((RectTransform)g.transform,rect);
            var t=g.GetComponent<TextMeshProUGUI>(); t.font=font; t.fontSize=size; t.color=color; t.text=value; t.richText=false; t.textWrappingMode=TextWrappingModes.Normal; t.overflowMode=TextOverflowModes.Overflow; t.raycastTarget=false; return t;
        }
        private Button Button(Transform parent,Rect rect,string label,Action click,float size=17)
        {
            var panel=Panel(parent,rect,new Color(.17f,.15f,.12f,.98f)); panel.name=label; var b=panel.AddComponent<Button>();
            var colors=b.colors; colors.highlightedColor=new Color(.76f,.70f,.55f); colors.selectedColor=new Color(.78f,.64f,.42f); b.colors=colors;
            var text=Text(panel.transform,new Rect(5,1,rect.width-10,rect.height-2),label,size,ink); text.alignment=TextAlignmentOptions.Center;
            panel.AddComponent<SliceFocusOutline>().Initialize(b); b.onClick.AddListener(()=>click()); return b;
        }
        private void Hold(Transform parent,Rect rect,string label,string tool)
        { var b=Button(parent,rect,label,()=>{},15); var h=b.gameObject.AddComponent<SliceHoldButton>(); h.Down=()=>owner.Input.Begin(tool); h.Up=()=>owner.Input.Release(); }
        public void RememberFocus()=>focus.Push(EventSystem.current?.currentSelectedGameObject);
        private static bool Available(GameObject target)
        { var selectable=target!=null ? target.GetComponent<Selectable>() : null; return selectable!=null && selectable.isActiveAndEnabled && selectable.IsInteractable(); }
        private GameObject ContextualFocus()
        {
            GameObject area=modal!=null && modal.activeInHierarchy ? modal : choicePanel.activeInHierarchy ? choicePanel : brewing.activeInHierarchy ? brewing : welcome.activeInHierarchy ? welcome : null;
            var candidate=area?.GetComponentsInChildren<Button>().FirstOrDefault(x=>Available(x.gameObject));
            if(candidate!=null) return candidate.gameObject;
            if(Available(next.gameObject)) return next.gameObject;
            return root.GetComponentsInChildren<Button>().FirstOrDefault(x=>Available(x.gameObject))?.gameObject;
        }
        private void RecoverFocus()
        {
            if(EventSystem.current==null || owner.Busy || owner.Session?.IsSaving==true) return;
            var selected=EventSystem.current.currentSelectedGameObject;
            if(!Available(selected) || (modal!=null && modal.activeInHierarchy && !selected.transform.IsChildOf(modal.transform)))
                EventSystem.current.SetSelectedGameObject(ContextualFocus());
        }
        public void RestoreFocus() { var old=focus.Count>0 ? focus.Pop() : null; EventSystem.current?.SetSelectedGameObject(Available(old) ? old : ContextualFocus()); }
        public void CloseMenu() { MenuModal=null; focus.Clear(); }
        public bool ActivateFocused(bool modalOnly=false)
        {
            var selected=EventSystem.current?.currentSelectedGameObject; var button=selected!=null ? selected.GetComponent<Button>() : null;
            if(modalOnly && !ModalOwnsFocus) return false;
            if(button!=null && button.isActiveAndEnabled && button.interactable) { button.onClick.Invoke(); return true; } return false;
        }
        public void Refresh()
        {
            var s=owner.Session; bool brew=s?.State.Phase==CampaignPhase.Brewing;
            bool expanded=s!=null && s.State.SlotIndex>=17 && owner.Content.Scope!="act_01";
            classicStir.SetActive(!expanded); expandedStir.SetActive(expanded);
            int shelfIndex=0; foreach(var item in shelf) {
                item.Value.gameObject.SetActive(s?.Supply.Contains(item.Key)==true);
                Position((RectTransform)item.Value.transform,new Rect(10,402-shelfIndex*(expanded ? 22.5f : 25),160,expanded ? 22 : 24)); shelfIndex++;
            }
            welcome.SetActive(s==null); dialogue.SetActive(s!=null && !brew); brewing.SetActive(brew); ingredients.SetActive(brew); requestPanel.SetActive(brew); choicePanel.SetActive(s?.State.Phase==CampaignPhase.Choice);
            continueButton.interactable=owner.Saved?.Checkpoint!=null && !owner.Busy;
            status.text=owner.Busy || s?.IsSaving==true ? "기록 중…" : owner.StartupError!=null ? "기록을 확인해 주세요: "+owner.StartupError : owner.Input.Feedback ?? "";
            title.text="설화   /   "+(expanded ? "두 번째 이야기" : "첫 번째 이야기")+(s==null ? "" : "   ·   "+s.CurrentEncounter.Night+"번째 밤");
            if(s!=null)
            {
                request.text=owner.Content.Texts[s.CurrentEncounter.SummaryTextId].Value;
                var phase=s.State.Phase; string name=owner.Content.Texts[owner.Content.Guests[s.CurrentEncounter.GuestId].DisplayTextId.Value].Value;
                if(s.CurrentEncounter.GuestIds.Count>1) name=string.Join(" · ",s.CurrentEncounter.GuestIds.Select(id=>owner.Content.Texts[owner.Content.Guests[id].DisplayTextId.Value].Value));
                speaker.text=s.CurrentText?.Role=="spoken" ? name : s.CurrentText?.Role=="request_summary" ? "손님이 바라는 한 잔" : phase==CampaignPhase.Result || phase==CampaignPhase.Discovery ? "내어드린 한 잔" : "";
                body.fontSize=owner.LargeText ? 23 : 21; body.color=owner.HighContrast ? Color.white : ink;
                body.text=s.CurrentText?.Value ?? (phase==CampaignPhase.Welcome ? "작은 바의 불을 켭니다.\n오늘은 어떤 이야기가 이곳에 머물까요." : phase==CampaignPhase.NightIntro ? "눈이 내려앉은 문턱을 쓸고, 잔을 꺼냅니다." :
                    phase==CampaignPhase.NightEnd ? "마지막 잔을 닦습니다.\n창밖의 눈은 조금 더 쌓여 있습니다." : phase==CampaignPhase.Arriving ? "눈길 끝에서 발소리가 다가옵니다." : phase==CampaignPhase.Departing ? "잠시, 문밖의 겨울을 듣습니다." :
                    phase==CampaignPhase.Complete ? (expanded ? "작은 그네 곁에 빈자리가 남아 있습니다.\n두 사람이 머물던 밤을 이곳에 남겨 둡니다." : s.State.Farewell==PrinceFarewell.Open ? "빈 의자 곁에 작은 별이 빛납니다.\n그가 머물던 밤을 이곳에 남겨 둡니다." : "빈 의자 곁에 작은 별이 놓여 있습니다.\n말하지 못한 것들도 이곳에 남겨 둡니다.") : phase==CampaignPhase.Retry ? "들었던 이야기를 도감에서 다시 살펴봅니다.\n새 잔으로 천천히 이어갈 수 있습니다." : ResultText());
                next.gameObject.SetActive(phase!=CampaignPhase.Arriving && phase!=CampaignPhase.Departing && phase!=CampaignPhase.Choice && phase!=CampaignPhase.Complete);
                next.interactable=s.CanInteract; next.GetComponentInChildren<TextMeshProUGUI>().text=phase==CampaignPhase.Request ? "한 잔 준비하기" : "계속  Enter";
                if(brew) measurement.text=owner.Content.IngredientNames[owner.Input.SelectedIngredient]+" → "+TargetName(owner.Input.Target)+"   |   지거 "+Ml(s.Brew.Container(ContainerRole.Jigger).TotalMl)+"   혼합 "+Ml(s.Brew.Container(ContainerRole.Mixing).TotalMl)+"   셰이커 "+Ml(s.Brew.Container(ContainerRole.Shaker).TotalMl)+"   서빙 "+Ml(s.Brew.Container(ContainerRole.Serving).TotalMl)+" ml"+(owner.Input.Fine ? " · 미세" : "")+(s.Brew.ShakerClosed ? " · 뚜껑 닫힘" : "");
            }
            string requested=s?.SaveError!=null ? "save_error" : s?.Modals.Top ?? MenuModal;
            if(requested!=shownModal) BuildModal(requested);
            RecoverFocus();
        }
        private static string Ml(decimal value)=>value.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture);
        private static string TargetName(ContainerRole role)=>role==ContainerRole.Jigger ? "지거" : role==ContainerRole.Mixing ? "혼합 잔" : role==ContainerRole.Shaker ? "셰이커" : "서빙 잔";
        private string ResultText()
        {
            var state=owner.Session.State; var o=state.CurrentService?.Outcome; if(o==null) return "";
            string name=(o.RecipeId==null ? null : owner.Session.Knowledge.Project(o.RecipeId).Name) ?? "아직 이름을 알 수 없는 한 잔";
            if(state.Phase==CampaignPhase.Discovery) return name+"\n만들어 본 한 잔을 도감에 기록했습니다.";
            string preparation=o.Preparation==PreparationResult.NotEvaluable ? "재료 구성을 다시 살펴봅니다." : o.Preparation!=PreparationResult.Good ? "도감의 준비 순서와 마무리를 돌아봅니다." : !o.QualityApplicable ? "잔 안에서 준비가 고르게 담겼습니다." : !o.QualityGood ? "조금 더 고른 손길을 남길 수 있겠습니다." : "준비와 손길이 고르게 담겼습니다.";
            return name+" · "+Ml(state.CurrentService.Snapshot.TotalMl)+" ml\n"+preparation+" "+(o.Fits ? "손님의 바람에 닿았습니다." : "손님의 바람과는 다른 결입니다.");
        }
        private void BuildModal(string kind)
        {
            shownModal=kind; if(modal!=null) { modal.SetActive(false); Destroy(modal); } if(kind==null) return;
            modal=Panel(root,new Rect(0,0,1280,720),new Color(.025f,.04f,.05f,.82f));
            var card=Panel(modal.transform,new Rect(174,54,932,604),new Color(paper.r,paper.g,paper.b,1)); Panel(card.transform,new Rect(28,524,876,2),gold);
            Text(card.transform,new Rect(30,540,800,40),kind=="codex" ? "한 잔의 기록" : kind=="guide" ? "손에 익히는 일" : kind=="settings" ? "소리와 읽기" : kind=="reset" ? "새로운 밤을 시작할까요?" : kind=="recovery" ? "이전 기록에서 이어 하기" : "기록을 저장하지 못했습니다",27,ink);
            if(kind=="codex") Book(card.transform);
            else if(kind=="guide") Text(card.transform,new Rect(36,106,860,390),
                "따르기 · Q를 누르는 동안 실제 양이 늘어납니다. Shift 또는 미세 따르기로 속도를 낮춥니다. 지거로 잰 뒤 대상을 고르고 E로 옮깁니다.\n\nStir · 혼합 잔에 얼음과 재료를 담고 S를 누른 채 잔 둘레를 돕니다. R로 거릅니다.\n\nBuild · 서빙 잔에서 얼음과 재료를 가볍게 섞습니다. G로 젓고 필요한 가니시를 놓습니다.\n\nShake · 셰이커에 재료와 얼음을 담고 L로 닫습니다. K와 좌우 움직임으로 흔든 뒤 열고 T로 옮깁니다. X로 선택한 재료를 조금씩 더할 수 있습니다.\n\nFloat · 재료마다 지거로 양을 잽니다. F를 누른 채 화면 중앙 아래에서 천천히 붓습니다. 오른쪽으로 갈수록 빠르고 위로 갈수록 높습니다.",19,ink);
            else if(kind=="settings")
            {
                Text(card.transform,new Rect(36,438,860,50),"소리와 글자 설정은 새로운 기록에서도 유지됩니다.",20,muted);
                Button(card.transform,new Rect(36,352,268,48),"소리 낮게",()=>owner.ChangeSettings(.3f,owner.LargeText,owner.HighContrast));
                Button(card.transform,new Rect(320,352,268,48),"소리 보통",()=>owner.ChangeSettings(.65f,owner.LargeText,owner.HighContrast));
                Button(card.transform,new Rect(604,352,292,48),"고요하게",()=>owner.ChangeSettings(0,owner.LargeText,owner.HighContrast));
                Button(card.transform,new Rect(36,282,410,48),"글자 크기 바꾸기",()=>{owner.ChangeSettings(owner.MasterVolume,!owner.LargeText,owner.HighContrast); BuildModal(kind);});
                Button(card.transform,new Rect(462,282,434,48),"글자 대비 바꾸기",()=>{owner.ChangeSettings(owner.MasterVolume,owner.LargeText,!owner.HighContrast); BuildModal(kind);});
                Button(card.transform,new Rect(36,190,268,48),"새로 시작…",()=>owner.OpenModal("reset"));
                Button(card.transform,new Rect(320,190,268,48),"저장된 기록으로 나가기",()=>owner.ReturnToSavedMenu());
                Button(card.transform,new Rect(604,190,292,48),"게임 종료",()=>UnityEngine.Application.Quit());
            }
            else if(kind=="reset")
            {
                Text(card.transform,new Rect(36,252,860,210),"현재 이야기의 진행과 발견을 새 기록으로 바꿉니다.\n소리와 읽기 설정은 유지됩니다.",23,ink);
                Button(card.transform,new Rect(36,160,410,52),"새 기록으로 시작",()=>owner.BeginNew(true));
            }
            else if(kind=="recovery")
            {
                Text(card.transform,new Rect(36,252,860,210),"최근 기록을 읽지 못했습니다. 이전에 확인된 기록으로 돌아갑니다.\n\n"+(owner.Saved?.Message ?? ""),21,ink);
                Button(card.transform,new Rect(36,160,410,52),"이전 기록으로 이어 하기",()=>owner.ContinueRun(true));
            }
            else
            {
                Text(card.transform,new Rect(36,252,860,210),"읽던 곳을 그대로 두었습니다. 저장할 수 있게 되면 다시 시도해 주세요.\n\n"+owner.Session?.SaveError,21,ink);
                Button(card.transform,new Rect(36,160,410,52),"저장 다시 시도",()=>owner.RetrySave());
                Button(card.transform,new Rect(462,160,434,52),"저장된 기록으로 나가기",()=>owner.ReturnToSavedMenu());
            }
            var close=kind!="save_error" ? Button(card.transform,new Rect(728,20,168,40),"돌아가기  Esc",()=>owner.CloseModal()) : card.GetComponentInChildren<Button>();
            var controls=card.GetComponentsInChildren<Button>(); foreach(var control in controls) { var nav=control.navigation; nav.mode=Navigation.Mode.Explicit;
                nav.selectOnLeft=Neighbor(control,controls,Vector2.left); nav.selectOnRight=Neighbor(control,controls,Vector2.right); nav.selectOnUp=Neighbor(control,controls,Vector2.up); nav.selectOnDown=Neighbor(control,controls,Vector2.down); control.navigation=nav; }
            EventSystem.current.SetSelectedGameObject(close.gameObject);
        }
        private void Book(Transform card)
        {
            var knowledge=owner.Session?.Knowledge ?? owner.Content.CreateKnowledge(); var ids=owner.Content.Catalog.Recipes.Keys.ToArray(); recipePage=Mathf.Clamp(recipePage,0,ids.Length-1);
            int index=0; foreach(string id in ids) { int page=index; var p=knowledge.Project(id); string label=p.Name ?? (p.Ingredients.Count>0 ? "들었던 한 잔" : "아직 빈 페이지");
                Button(card,new Rect(30,446-index*58,242,48),label,()=>{recipePage=page; BuildModal("codex");},18).name="Recipe page "+index; index++; }
            Panel(card,new Rect(294,94,2,408),gold);
            var projection=knowledge.Project(ids[recipePage]); var recipe=owner.Content.Catalog.Recipes[ids[recipePage]];
            Text(card,new Rect(322,454,568,50),projection.Name ?? "이름을 기다리는 한 잔",28,ink);
            Text(card,new Rect(322,416,568,32),(projection.Known ? "만들어 본 기록" : "대화 속에서 들은 단서")+"    ·    "+(projection.Method ?? "제조법 미상"),16,muted);
            string amounts=projection.Ingredients.Count==0 ? "아직 적어 둔 재료가 없습니다." : string.Join("\n",projection.Ingredients.Select(id=>owner.Content.IngredientNames[id]+"    "+(projection.Amounts.TryGetValue(id,out var ml) ? Ml(ml)+" ml" : "양 미상")+(projection.PreparationVisible && recipe.DashIngredient==id ? " · Dash" : "")));
            Text(card,new Rect(322,254,568,150),amounts,owner.LargeText ? 22 : 20,ink);
            string preparation="준비 순서는 아직 듣지 못했습니다.";
            if(projection.PreparationVisible) preparation=recipe.Method=="stir" ? "혼합 잔에 얼음 · 재료를 담은 뒤 원을 그리며 젓기\n서빙 잔에 거르기" : recipe.Method=="build" ? "서빙 잔에 얼음과 재료 · 가볍게 젓기" : recipe.Method=="shake" ? "소다를 제외한 재료와 얼음을 셰이커에\n뚜껑을 닫고 흔들기 · 열어 잔에 옮기기\n소다는 마지막에 Splash · 서빙 얼음 없음" : "보드카 → 커피 리큐어 → 크림\n각 재료를 낮게, 천천히 층으로 쌓기 · 얼음 없음";
            if(projection.GarnishVisible) preparation+="\n"+(recipe.RequiredGarnish=="cherry" ? "체리 · 잔 안" : recipe.RequiredGarnish=="orange_slice" ? "오렌지 슬라이스 · 잔 가장자리" : recipe.RequiredGarnish=="orange_zest" ? "오렌지 제스트 · 잔 가장자리" : "가니시 생략 가능");
            Text(card,new Rect(322,104,568,138),preparation,owner.LargeText ? 20 : 18,ink);
            Text(card,new Rect(30,26,650,30),"발견은 이름을 남깁니다. 단서는 그 전의 길을 비춥니다.",15,muted);
        }
        private static Selectable Neighbor(Button origin,Button[] controls,Vector2 direction)
        {
            Vector2 center=origin.transform.position; Button best=origin; float score=0;
            foreach(var c in controls) { if(c==origin || !c.IsInteractable()) continue; Vector2 delta=(Vector2)c.transform.position-center; float forward=Vector2.Dot(delta,direction);
                if(forward>0 && forward/delta.sqrMagnitude>score) { score=forward/delta.sqrMagnitude; best=c; } } return best;
        }
    }
}
