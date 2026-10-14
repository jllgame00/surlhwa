using System.Reflection;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.TestTools;
using Surlhwa.Presentation;

namespace Surlhwa.SpatialTests
{
    public sealed class SliceFocusTests
    {
        [UnityTest] public IEnumerator KeyboardSelectionShowsAnOutlineWithoutActivatingOrBlockingTheControl()
        {
            yield return new EnterPlayMode();
            var canvas=new GameObject("Focus test canvas",typeof(RectTransform),typeof(Canvas));
            var events=new GameObject("Focus test events",typeof(EventSystem));
            try {
                var ui=canvas.AddComponent<SliceUi>(); int activated=0;
                var factory=typeof(SliceUi).GetMethod("Button",BindingFlags.NonPublic|BindingFlags.Instance);
                var button=(Button)factory.Invoke(ui,new object[]{canvas.transform,new Rect(0,0,130,42),"Continue",(System.Action)(()=>activated++),17f});
                var marker=button.transform.Find("Focus outline");
                Assert.That(marker,Is.Not.Null,"Keyboard focus needs a shape, not only selectedColor.");
                Assert.That(marker.gameObject.activeSelf,Is.False);
                EventSystem.current.SetSelectedGameObject(button.gameObject);
                Assert.That(marker.gameObject.activeSelf,Is.True);
                Assert.That(activated,Is.Zero,"Selection is not activation.");
                foreach(var image in marker.GetComponentsInChildren<Image>(true)) Assert.That(image.raycastTarget,Is.False);
                Assert.That(button.targetGraphic.raycastTarget,Is.True);
                EventSystem.current.SetSelectedGameObject(null);
                Assert.That(marker.gameObject.activeSelf,Is.False);
                button.interactable=false;
                EventSystem.current.SetSelectedGameObject(button.gameObject);
                Assert.That(marker.gameObject.activeSelf,Is.False,"Unavailable buttons must not promise activation.");
            }
            finally { Object.DestroyImmediate(canvas); Object.DestroyImmediate(events); }
            yield return new ExitPlayMode();
        }
        [UnityTest] public IEnumerator ModalNavigationCannotSelectAControlBehindThePanel()
        {
            yield return new EnterPlayMode();
            var canvas=new GameObject("Modal test canvas",typeof(RectTransform),typeof(Canvas));
            var events=new GameObject("Modal test events",typeof(EventSystem));
            GameObject modal=null;
            try {
                var ui=canvas.AddComponent<SliceUi>();
                typeof(SliceUi).GetField("root",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(ui,canvas.GetComponent<RectTransform>());
                var factory=typeof(SliceUi).GetMethod("Button",BindingFlags.NonPublic|BindingFlags.Instance);
                factory.Invoke(ui,new object[]{canvas.transform,new Rect(612,94,138,42),"Background Stir",(System.Action)(()=>Assert.Fail("Background activated")),17f});
                typeof(SliceUi).GetMethod("BuildModal",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(ui,new object[]{"settings"});
                modal=(GameObject)typeof(SliceUi).GetField("modal",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(ui);
                Canvas.ForceUpdateCanvases();
                var selected=EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>();
                var left=selected.FindSelectableOnLeft();
                Assert.That(left,Is.Not.Null);
                Assert.That(left.transform.IsChildOf(modal.transform),Is.True,"Modal keyboard focus escaped behind its blocking panel.");
                foreach(var control in modal.GetComponentsInChildren<Selectable>())
                    foreach(var neighbor in new[]{control.FindSelectableOnLeft(),control.FindSelectableOnRight(),control.FindSelectableOnUp(),control.FindSelectableOnDown()})
                        Assert.That(neighbor==null || neighbor.transform.IsChildOf(modal.transform),Is.True);
            }
            finally { Object.DestroyImmediate(canvas); Object.DestroyImmediate(events); if(modal!=null) Object.DestroyImmediate(modal); }
            yield return new ExitPlayMode();
        }
    }
}
