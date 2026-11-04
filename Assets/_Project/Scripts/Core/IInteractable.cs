namespace VirtualOffice.Core
{
    /// Implemented by anything the player can focus (via InteractionSystem's raycast)
    /// and trigger with the interact key.
    public interface IInteractable
    {
        string InteractionPrompt { get; }
        bool CanInteract { get; }
        void Interact(FirstPersonController interactor);
    }
}
