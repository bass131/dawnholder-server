#nullable enable
using UnityEngine;
using UnityEngine.EventSystems;

namespace Dawnholder.Client.UI
{
    /// <summary>
    /// Takes the pointer drags on a window's grab area and hands each step to its <see cref="UiWindowPlacement"/>. It sits
    /// on the grab area itself rather than the window, so a drag that starts on a button over the title bar never
    /// reaches it and the button keeps its own press.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class UiWindowGrabArea : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        UiWindowPlacement _placement = null!;

        internal static UiWindowGrabArea Attach(GameObject grabArea, UiWindowPlacement placement)
        {
            var handler = grabArea.AddComponent<UiWindowGrabArea>();
            handler._placement = placement;
            return handler;
        }

        // The input module begins a drag only once the pointer passed the drag threshold; the way there moves too.
        public void OnBeginDrag(PointerEventData eventData) =>
            _placement.MoveBy(eventData.pressPosition, eventData.position - eventData.delta);

        public void OnDrag(PointerEventData eventData) =>
            _placement.MoveBy(eventData.position - eventData.delta, eventData.position);

        // Every step already stored and showed its move; ending the drag leaves nothing to finish.
        public void OnEndDrag(PointerEventData eventData)
        {
        }
    }
}
