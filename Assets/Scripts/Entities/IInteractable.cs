public interface IInteractable
{
    public bool Interact()
    {
        if (canInteract) return Interaction();
        else return false;
    }
    protected bool canInteract { get; }
    protected bool Interaction();
}