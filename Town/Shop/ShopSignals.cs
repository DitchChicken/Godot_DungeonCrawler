using System;

public static class ShopSignals
{
	// Shop scene subscribes; transfer layer raises.
	public static event Action<string> MessageRaised;
	// Fired after any buy/sell so the shop can refresh.
	public static event Action TransactionCompleted;

	public static void RaiseMessage(string msg) => MessageRaised?.Invoke(msg);
	public static void RaiseTransaction()       => TransactionCompleted?.Invoke();
}
