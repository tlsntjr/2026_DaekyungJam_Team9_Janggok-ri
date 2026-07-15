public interface IInteractable
{
	string Prompt { get; }		// "E: 조사" 등 HUD 표시용
	void Interact();
}