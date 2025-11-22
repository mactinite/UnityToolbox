using UnityEngine.EventSystems;

namespace mactinite.TooolboxCommons.Utilities
{
    public interface IDragAndDropHandler : IDragHandler, IDropHandler, IEndDragHandler, IBeginDragHandler
    {
        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData) {}
        void IEndDragHandler.OnEndDrag(PointerEventData eventData) {}
        void IDragHandler.OnDrag(PointerEventData eventData) {}
        void IDropHandler.OnDrop(PointerEventData eventData) {}
    }
    
    public interface IHoverHandler : IPointerEnterHandler, IPointerExitHandler {}
    
}