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
    public sealed class SliceUi : MonoBehaviour
    {
        private SliceController owner; private TMP_FontAsset font; private RectTransform root;
        private GameObject welcome,dialogue,brewing,ingredients,modal,requestPanel,discard; private TextMeshProUGUI speaker,body,request,measurement,status,title;
        private Button next,continueButton; private readonly Stack<GameObject> focus=new Stack<GameObject>();
        private string shownModal; public string MenuModal;
        public string VisibleModal => shownModal;
        public bool ModalOwnsFocus => modal!=null && EventSystem.current?.currentSelectedGameObject!=null && EventSystem.current.currentSelectedGameObject.transform.IsChildOf(modal.transform);
        public string DisplayedSpeaker => speaker.text;
        private readonly Color ink=new Color(.90f,.84f,.72f),muted=new Color(.67f,.72f,.73f),paper=new Color(.075f,.09f,.10f,.96f);
        public void Initialize(SliceController controller,TMP_FontAsset face)
        {
            owner=controller; font=face;
            var canvas=new GameObject("Slice reading and tools",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)); root=canvas.GetComponent<RectTransform>(); root.SetParent(transform,false);
            var surface=canvas.GetComponent<Canvas>(); surface.renderMode=RenderMode.ScreenSpaceCamera; surface.worldCamera=controller.Camera; surface.planeDistance=1; surface.sortingOrder=100;
            var scaler=canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1280,720); scaler.matchWidthOrHeight=.5f;
            var events=new GameObject("Slice input focus",typeof(EventSystem),typeof(InputSystemUIInputModule)); events.transform.SetParent(transform);
            var module=events.GetComponent<InputSystemUIInputModule>(); module.AssignDefaultActions(); module.submit=null; module.cancel=null;
            title=Text(root,new Rect(28,663,700,36),"설화  /  ACT 1 · Stir 발췌",20,ink);
            Button(root,new Rect(907,663,102,36),"Guide  B",()=>owner.OpenModal("guide"));
            Button(root,new Rect(1017,663,102,36),"Codex  Tab",()=>owner.OpenModal("codex"));
            Button(root,new Rect(1127,663,125,36),"설정  Esc",()=>owner.OpenModal("settings"));
            requestPanel=Panel(root,new Rect(282,602,716,52),paper);
            request=Text(requestPanel.transform,new Rect(12,4,692,44),"",18,ink); request.alignment=TextAlignmentOptions.Center;
            status=Text(root,new Rect(28,630,260,30),"",16,muted);
            welcome=Panel(root,new Rect(306,190,668,326),paper);
            Text(welcome.transform,new Rect(32,239,604,55),"설화",34,ink);
            Text(welcome.transform,new Rect(32,92,604,148),owner.Content.Profile.FixtureContext,20,ink);
            var newGame=Button(welcome.transform,new Rect(32,24,184,48),"새로 시작",()=>owner.BeginNew());
            continueButton=Button(welcome.transform,new Rect(236,24,184,48),"이어 하기",()=>owner.ContinueRun());
            dialogue=Panel(root,new Rect(222,24,836,156),paper);
            speaker=Text(dialogue.transform,new Rect(24,116,788,30),"",18,muted);
            body=Text(dialogue.transform,new Rect(24,44,788,72),"",21,ink);
            next=Button(dialogue.transform,new Rect(682,9,130,36),"계속  Enter",()=>owner.Advance());
            brewing=Panel(root,new Rect(222,22,1030,164),paper);
            measurement=Text(brewing.transform,new Rect(18,124,992,28),"",17,ink);
            Hold(brewing.transform,new Rect(18,72,108,42),"따르기  Q","pour"); Hold(brewing.transform,new Rect(134,72,132,42),"지거 이동  E","transfer");
            Hold(brewing.transform,new Rect(274,72,108,42),"거르기  R","strain"); Hold(brewing.transform,new Rect(390,72,138,42),"젓기  S + 원","stir");
            Button(brewing.transform,new Rect(536,72,100,42),"얼음  I",()=>owner.Input.Command("ice")); Button(brewing.transform,new Rect(644,72,100,42),"Dash  D",()=>owner.Input.Command("dash"));
            Button(brewing.transform,new Rect(752,72,100,42),"체리  C",()=>owner.Input.Command("cherry")); Button(brewing.transform,new Rect(860,72,150,42),"손님께 내기",()=>owner.Advance());
            Button(brewing.transform,new Rect(18,16,134,42),"대상 변경  J",()=>owner.Input.Command("target"));
            Button(brewing.transform,new Rect(160,16,138,42),"서빙 잔  V",()=>owner.HandleCommand("glass"));
            Button(brewing.transform,new Rect(306,16,148,42),"미세 따르기",()=>owner.Input.Fine=!owner.Input.Fine);
            Button(brewing.transform,new Rect(462,16,154,42),"도구 깊이 비교",()=>owner.Workbench.FlatTools=!owner.Workbench.FlatTools);
            Text(brewing.transform,new Rect(632,12,378,46),"누르는 동안만 따릅니다.\nS를 누르고 혼합 잔 둘레를 천천히 도세요.",15,muted);
            ingredients=Panel(root,new Rect(24,218,184,376),paper); Text(ingredients.transform,new Rect(12,336,160,30),"재료 선반",18,ink);
            int n=0; foreach(var id in owner.Content.Profile.Supply) { string ingredient=id; Button(ingredients.transform,new Rect(10,298-n*40,164,35),(n+1)+"  "+owner.Content.IngredientNames[id],()=>owner.Input.SelectIngredient(ingredient),15); n++; }
            discard=Button(root,new Rect(24,152,184,44),"현재 잔 비우기",()=>owner.DiscardDrink()).gameObject;
            Refresh(); EventSystem.current.SetSelectedGameObject(newGame.gameObject);
        }
        private GameObject Panel(Transform parent,Rect rect,Color color)
        { var g=new GameObject("Panel",typeof(RectTransform),typeof(Image)); g.transform.SetParent(parent,false); Position((RectTransform)g.transform,rect); g.GetComponent<Image>().color=color; return g; }
        private static void Position(RectTransform t,Rect r) { t.anchorMin=t.anchorMax=Vector2.zero; t.pivot=Vector2.zero; t.anchoredPosition=r.position; t.sizeDelta=r.size; }
        private TextMeshProUGUI Text(Transform parent,Rect rect,string value,float size,Color color)
        {
            var g=new GameObject("Text",typeof(RectTransform),typeof(TextMeshProUGUI)); g.transform.SetParent(parent,false); Position((RectTransform)g.transform,rect);
            var text=g.GetComponent<TextMeshProUGUI>(); text.font=font; text.fontSize=size; text.color=color; text.text=value; text.richText=false;
            text.textWrappingMode=TextWrappingModes.Normal; text.overflowMode=TextOverflowModes.Overflow; text.raycastTarget=false; return text;
        }
        private Button Button(Transform parent,Rect rect,string label,Action click,float size=17)
        {
            var panel=Panel(parent,rect,new Color(.16f,.20f,.21f,.98f)); panel.name=label; var button=panel.AddComponent<Button>();
            var colors=button.colors; colors.highlightedColor=new Color(.71f,.66f,.48f); colors.selectedColor=new Color(.74f,.63f,.40f); button.colors=colors;
            var t=Text(panel.transform,new Rect(4,1,rect.width-8,rect.height-2),label,size,ink); t.alignment=TextAlignmentOptions.Center;
            panel.AddComponent<SliceFocusOutline>().Initialize(button);
            button.onClick.AddListener(()=>click()); return button;
        }
        private void Hold(Transform parent,Rect rect,string label,string tool)
        { var b=Button(parent,rect,label,()=>{}); var h=b.gameObject.AddComponent<SliceHoldButton>(); h.Down=()=>owner.Input.Begin(tool); h.Up=()=>owner.Input.Release(); }
        public void RememberFocus() { focus.Push(EventSystem.current?.currentSelectedGameObject); }
        public void RestoreFocus() { var previous=focus.Count>0 ? focus.Pop() : null; EventSystem.current?.SetSelectedGameObject(previous!=null && previous.activeInHierarchy ? previous : next.gameObject); }
        public void CloseMenu() { MenuModal=null; focus.Clear(); }
        public bool ActivateFocused(bool modalOnly=false)
        {
            var selected=EventSystem.current?.currentSelectedGameObject; var button=selected!=null ? selected.GetComponent<Button>() : null;
            if(modalOnly && !ModalOwnsFocus) return false;
            if(button!=null && button.isActiveAndEnabled && button.interactable) { button.onClick.Invoke(); return true; }
            return false;
        }
        public void Refresh()
        {
            var s=owner.Session; bool brew=s?.State.Phase==SlicePhase.Brewing;
            welcome.SetActive(s==null); dialogue.SetActive(s!=null && !brew); brewing.SetActive(brew); ingredients.SetActive(brew); discard.SetActive(brew);
            continueButton.interactable=owner.Saved?.Checkpoint!=null && !owner.Busy;
            status.text=owner.Busy || s?.IsSaving==true ? "기록 중…" : owner.StartupError!=null ? "기록을 확인해 주세요" : owner.Input.Feedback ?? "";
            request.text=s!=null && s.State.Phase!=SlicePhase.Welcome && s.State.Phase!=SlicePhase.Complete ? owner.Content.Texts[owner.Content.Requests[s.CurrentEncounter.RequestId].SummaryTextId].Value : "";
            requestPanel.SetActive(brew);
            if(s!=null)
            {
                string name=owner.Content.Texts[owner.Content.Guests[s.CurrentEncounter.GuestId].DisplayTextId.Value].Value;
                speaker.text=s.CurrentText==null ? (s.State.Phase==SlicePhase.Welcome ? "ACT 1 · 발췌 안내" : s.State.Phase==SlicePhase.Result || s.State.Phase==SlicePhase.Discovery ? "내어드린 한 잔" : "") : s.CurrentText.Role=="spoken" ? name : s.CurrentText.Role=="request_summary" ? "손님이 바라는 한 잔" : "";
                body.fontSize=owner.LargeText ? 22 : 21; body.color=owner.HighContrast ? Color.white : ink;
                body.text=s.CurrentText?.Value ?? (s.State.Phase==SlicePhase.Welcome ? "평범한 손님을 맞은 뒤, 어린왕자의 온기를 바라는 방문을 이어 봅니다.\nACT의 끝과 기억을 얻는 장면은 이 발췌에 포함되지 않습니다." : s.State.Phase==SlicePhase.Arriving ? "눈길 끝에서 발소리가 다가옵니다." :
                    s.State.Phase==SlicePhase.Departing ? "잠시, 문밖의 겨울을 듣습니다." : s.State.Phase==SlicePhase.Complete ? "오늘의 발췌가 끝났습니다.\nACT 1의 끝과 기억은 아직 남아 있습니다." : ResultText());
                next.gameObject.SetActive(s.State.Phase!=SlicePhase.Arriving && s.State.Phase!=SlicePhase.Departing && s.State.Phase!=SlicePhase.Complete);
                next.interactable=s.CanInteract;
                next.GetComponentInChildren<TextMeshProUGUI>().text=s.State.Phase==SlicePhase.Request ? "Stir 준비" : "계속  Enter";
                if(brew) measurement.text=owner.Content.IngredientNames[owner.Input.SelectedIngredient]+"  →  "+TargetName(owner.Input.Target)+"    |    지거 "+Ml(s.Brew.Container(ContainerRole.Jigger).TotalMl)+" / 60    혼합 "+Ml(s.Brew.Container(ContainerRole.Mixing).TotalMl)+" / 300    서빙 "+Ml(s.Brew.Container(ContainerRole.Serving).TotalMl)+" / 180 ml"+(owner.Input.Fine ? "  · 미세" : "");
            }
            string requested=s?.SaveError!=null ? "save_error" : s?.Modals.Top ?? MenuModal;
            if(requested!=shownModal) BuildModal(requested);
        }
        private static string Ml(decimal value) => value.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture);
        private static string TargetName(ContainerRole role) => role==ContainerRole.Jigger ? "지거" : role==ContainerRole.Mixing ? "혼합 잔" : "서빙 잔";
        private string ResultText()
        {
            var state=owner.Session.State; var o=state.CurrentService?.Outcome; if(o==null) return "";
            string name=o.CocktailId==null ? "아직 이름을 알 수 없는 한 잔" : owner.Content.Catalog.Cocktails[o.CocktailId].Name;
            if(state.Phase==SlicePhase.Discovery) return name+"\n만들어 본 한 잔을 도감에 기록했습니다.";
            string quality=o.Preparation==PreparationResult.NotEvaluable ? "재료의 구성을 다시 살펴볼 수 있습니다." : o.Preparation!=PreparationResult.Good ? "얼음·젓기·거르기와 마무리를 돌아봅니다." : !o.QualityGood ? "조금 더 고른 손길을 남길 수 있겠습니다." : "준비와 손길이 고르게 담겼습니다.";
            return name+"  ·  "+Ml(state.CurrentService.Snapshot.TotalMl)+" ml\n"+quality+" "+(o.Fits ? "바라던 온기에 닿았습니다." : "손님의 바람과는 다른 결입니다.");
        }
        private void BuildModal(string kind)
        {
            shownModal=kind; if(modal!=null) Destroy(modal); if(kind==null) return;
            bool book=kind=="guide" || kind=="codex";
            modal=Panel(root,new Rect(0,0,1280,720),new Color(0,0,0,.54f)); var card=Panel(modal.transform,book ? new Rect(278,66,724,582) : new Rect(278,110,724,498),paper);
            string heading=kind=="guide" ? "Guide · 지금의 한 잔" : kind=="codex" ? "Codex · 기억해 둔 제조법" : kind=="settings" ? "방의 소리와 읽기" : kind=="reset" ? "새로운 발췌를 시작할까요?" : kind=="recovery" ? "이전 기록에서 이어 하기" : "기록을 저장하지 못했습니다";
            Text(card.transform,new Rect(24,book ? 518 : 434,676,42),heading,24,ink);
            Button close=null;
            if(kind=="guide" || kind=="codex")
            {
                var knowledge=owner.Session?.Knowledge ?? owner.Content.CreateKnowledge(); int column=0;
                foreach(string id in owner.Content.Catalog.Recipes.Keys)
                {
                    var p=knowledge.Project(id); var recipe=owner.Content.Catalog.Recipes[id];
                    string content=p.Name ?? "이름을 모르는 레시피"; content+="\n\n"+(p.Method ?? "제조법 미상")+"\n";
                    foreach(var a in p.Ingredients) content+="\n"+owner.Content.IngredientNames[a]+(p.Amounts.TryGetValue(a,out var ml) ? "  "+ml.ToString("0.######",System.Globalization.CultureInfo.InvariantCulture)+" ml"+(p.PreparationVisible && recipe.DashIngredient==a ? " · Dash" : "") : "  양 미상");
                    if(p.PreparationVisible) content+="\n\n혼합 잔에 얼음\n원을 그리며 젓기\n서빙 잔에 거르기";
                    if(p.GarnishVisible) content+=recipe.RequiredGarnish=="cherry" ? "\n체리 · 잔 안" : "\n가니시 생략 가능";
                    Text(card.transform,new Rect(24+column*228,96,210,414),content,owner.LargeText ? 19 : 17,ink); column++;
                }
            }
            else if(kind=="settings")
            {
                Text(card.transform,new Rect(28,346,668,58),"소리와 글자 설정은 새로 시작해도 유지됩니다.",20,ink);
                Button(card.transform,new Rect(28,270,202,48),"소리 낮게",()=>owner.ChangeSettings(.3f,owner.LargeText,owner.HighContrast));
                Button(card.transform,new Rect(246,270,202,48),"소리 보통",()=>owner.ChangeSettings(.65f,owner.LargeText,owner.HighContrast));
                Button(card.transform,new Rect(464,270,232,48),"고요하게",()=>owner.ChangeSettings(0,owner.LargeText,owner.HighContrast));
                Button(card.transform,new Rect(28,202,310,48),"글자 크기 바꾸기",()=>owner.ChangeSettings(owner.MasterVolume,!owner.LargeText,owner.HighContrast));
                Button(card.transform,new Rect(354,202,342,48),"글자 대비 바꾸기",()=>owner.ChangeSettings(owner.MasterVolume,owner.LargeText,!owner.HighContrast));
                Button(card.transform,new Rect(28,128,310,48),"새로 시작…",()=>owner.OpenModal("reset"));
                Button(card.transform,new Rect(354,128,342,48),"나가기",()=>UnityEngine.Application.Quit());
            }
            else if(kind=="reset") {
                Text(card.transform,new Rect(28,234,668,160),"현재 발췌의 진행과 발견은 새 기록으로 바뀝니다.\n소리와 읽기 설정은 유지됩니다.",22,ink);
                Button(card.transform,new Rect(28,128,310,48),"새 기록으로 시작",()=>owner.BeginNew(true));
            }
            else if(kind=="recovery") {
                Text(card.transform,new Rect(28,202,668,184),"최근 기록을 읽지 못했습니다. 이전에 확인된 기록으로 돌아갑니다.\n\n"+(owner.Saved?.Message ?? ""),20,ink);
                Button(card.transform,new Rect(28,128,310,48),"이전 기록으로 이어 하기",()=>owner.ContinueRun(true));
            }
            else {
                Text(card.transform,new Rect(28,190,668,210),"읽던 곳을 그대로 두었습니다. 저장할 수 있게 되면 다시 시도해 주세요.\n\n"+owner.Session?.SaveError,20,ink);
                close=Button(card.transform,new Rect(28,54,310,48),"저장 다시 시도",()=>owner.RetrySave());
                Button(card.transform,new Rect(354,54,342,48),"저장된 기록으로 나가기",()=>owner.ReturnToSavedMenu());
            }
            if(kind!="save_error") close=Button(card.transform,new Rect(534,20,162,42),"돌아가기  Esc",()=>owner.CloseModal());
            var controls=card.GetComponentsInChildren<Button>();
            foreach(var control in controls) {
                var navigation=control.navigation; navigation.mode=Navigation.Mode.Explicit;
                navigation.selectOnLeft=ModalNeighbor(control,controls,Vector2.left); navigation.selectOnRight=ModalNeighbor(control,controls,Vector2.right);
                navigation.selectOnUp=ModalNeighbor(control,controls,Vector2.up); navigation.selectOnDown=ModalNeighbor(control,controls,Vector2.down);
                control.navigation=navigation;
            }
            if(close!=null) EventSystem.current.SetSelectedGameObject(close.gameObject);
        }
        private static Selectable ModalNeighbor(Button origin,Button[] controls,Vector2 direction)
        {
            var rect=(RectTransform)origin.transform; Vector2 center=rect.anchoredPosition+rect.sizeDelta*.5f;
            Button best=origin; float bestScore=0;
            foreach(var candidate in controls) {
                if(candidate==origin || !candidate.IsInteractable()) continue;
                var other=(RectTransform)candidate.transform; Vector2 delta=other.anchoredPosition+other.sizeDelta*.5f-center;
                float forward=Vector2.Dot(delta,direction); if(forward<=0) continue;
                float score=forward/delta.sqrMagnitude; if(score>bestScore) { best=candidate; bestScore=score; }
            }
            return best;
        }
    }
    public sealed class SliceFocusOutline : MonoBehaviour,ISelectHandler,IDeselectHandler
    {
        private Button button; private GameObject marker;
        public void Initialize(Button value)
        {
            button=value; marker=new GameObject("Focus outline",typeof(RectTransform));
            var rect=marker.GetComponent<RectTransform>(); rect.SetParent(transform,false);
            rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero;
            Edge("Top",new Vector2(0,1),Vector2.one,new Vector2(0,-1),new Vector2(0,2));
            Edge("Bottom",Vector2.zero,new Vector2(1,0),new Vector2(0,1),new Vector2(0,2));
            Edge("Left",Vector2.zero,new Vector2(0,1),new Vector2(1,0),new Vector2(2,0));
            Edge("Right",new Vector2(1,0),Vector2.one,new Vector2(-1,0),new Vector2(2,0));
            RefreshFocus();
        }
        private void Edge(string name,Vector2 min,Vector2 max,Vector2 position,Vector2 size)
        {
            var edge=new GameObject(name,typeof(RectTransform),typeof(Image));
            var r=edge.GetComponent<RectTransform>(); r.SetParent(marker.transform,false);
            r.anchorMin=min; r.anchorMax=max; r.anchoredPosition=position; r.sizeDelta=size;
            var image=edge.GetComponent<Image>(); image.color=new Color(.90f,.84f,.72f); image.raycastTarget=false;
        }
        private void RefreshFocus()
        {
            if(marker==null) return;
            bool visible=isActiveAndEnabled && button!=null && button.isActiveAndEnabled && button.IsInteractable() && EventSystem.current!=null && EventSystem.current.currentSelectedGameObject==gameObject;
            if(marker.activeSelf!=visible) marker.SetActive(visible);
        }
        public void OnSelect(BaseEventData data) { RefreshFocus(); }
        public void OnDeselect(BaseEventData data) { if(marker!=null) marker.SetActive(false); }
        private void LateUpdate() { RefreshFocus(); }
        private void OnDisable() { if(marker!=null) marker.SetActive(false); }
    }
    public sealed class SliceHoldButton : MonoBehaviour,IPointerDownHandler,IPointerUpHandler
    {
        public Action Down,Up;
        private Button button; private bool pressed;
        private bool Available()
        { if(button==null) button=GetComponent<Button>(); return isActiveAndEnabled && button!=null && button.isActiveAndEnabled && button.IsInteractable(); }
        public void OnPointerDown(PointerEventData data)
        { if(data.button==PointerEventData.InputButton.Left && !pressed && Available()) { pressed=true; Down?.Invoke(); } }
        public void OnPointerUp(PointerEventData data) { if(data.button==PointerEventData.InputButton.Left) Release(); }
        // Disabling a sibling Button does not send OnDisable to this handler.
        private void LateUpdate() { if(pressed && !Available()) Release(); }
        private void Release() { pressed=false; Up?.Invoke(); }
        private void OnDisable() { Release(); }
    }
}
