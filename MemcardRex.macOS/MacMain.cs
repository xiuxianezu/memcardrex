using AppKit;

namespace MemcardRex
{
	static class MainClass
	{
		static void Main (string [] args)
		{
			NSApplication.Init ();
			Localization.Initialize ();
			NSApplication.Main (args);
		}
	}
}
