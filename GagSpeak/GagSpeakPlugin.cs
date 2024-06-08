using Dalamud.Plugin;     // Used for the IDalamudPlugin interface
using OtterGui.Classes;   // For the messageService, which is used to display the changelog
using OtterGui.Log;       // for our plugin logger
using OtterGui.Services;  // REQUIRES FOR THE SERVICE MANAGER CLASS
using GagSpeak.UI;        // REQUIRED for our plugins GagSpeakWindowManager requiredservices to be fetched
using GagSpeak.Services;  // REQUIRED for our plugins CommandManager requiredservices to be fetched
using GagSpeak.Interop;
using GagSpeak.ChatMessages;
using GagSpeak.ChatMessages.ChatControl;
using GagSpeak.Hardcore.Actions;
using GagSpeak.Hardcore.Movement;
using GagSpeak.Hardcore;


// The main namespace for the plugin, aka the same name of our plugin, the highest level
namespace GagSpeak;

public class GagSpeak : IDalamudPlugin
{
  /// <summary> Gets the name of the plugin. </summary>
  public static string Name => "GagSpeak"; // Define plugin name

  /// <summary> gets the version of our current plugin from the .csproj file </summary>
  public static readonly string Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty;
  
  /// <summary> initialize the logger for our plugin, which will display debug information to our /xllog in game </summary>
  public static readonly Logger Log = new(); // dalamudside logger

  /// <summary> the messager service for this plugin. A part of the otterGui services, and is used to display the changelog </summary>
  public static MessageService Messager { get; private set; } = null!; // initialize the messager service, part of otterGui services.
  
  /// <summary> the service provider for the plugin </summary>
  public static ServiceManager _services { get; private set; } = null!; // initialize the service provider for the plugin

  /// <summary> The primary plugin constructor
  public GagSpeak(DalamudPluginInterface pluginInt)
  {
      // Initialize the services in the large Service collection. (see ServiceHandler.cs)
      // if at any point this process fails, we should immidiately dispose of the services, throw an exception, and exit the plugin.
      try
      {
          _services = ServiceHandler.CreateProvider(pluginInt, Log); // Initialize the services in the large Service collection. (see ServiceHandler.cs)
          Messager = _services.GetService<MessageService>(); // Initialize messager service here
          _services.EnsureRequiredServices();
          /* Big Knowledge Info Time:
           The services we initialize here, are the classes that are not called upon by any other class in our Gagspeak plugin.
           This is because our service constructor does "lazy initialization", meaning it wont initialize the classes if they
           are never called upon, but rather stand alone. Because it is an event we only invoke and do not interact with, we have to call it here.
          
           As for why it invokes the safewordcommand used and not the info request event, idk, im still figuring that out.
           All I know is that if you have a class struggling to initialize, you can call it here.
          */ 
          _services.GetService<GagSpeakWindowManager>();  // Initialize the UI
          _services.GetService<CommandManager>();         // Initialize the command manager
          _services.GetService<OnChatMsgManager>();       // Initialize the OnChatMessage
          _services.GetService<ChatInputProcessor>();     // Initialize the chat message detour
          _services.GetService<InfoRequestService>();     // Because the info request service is being a stubborn bitch and needs to subscribe to events and not be lazy.
          _services.GetService<GlamourerFunctions>();     // force loading here because nhothing else loads it so it is initialized as lazy
          _services.GetService<OnFrameworkService>();     // get the charahandler
          // for hardcore stuff
          _services.GetService<GsActionManager>();        // Get the action manager
          _services.GetService<MovementManager>();        // get the movement manager
          _services.GetService<OptionPromptListeners>();  // Make sure we are listening for the interactions
          GSLogger.LogType.Information($"GagSpeak v{Version} loaded successfully."); // Log the version to the /xllog menu
      }
      catch
      {
          Dispose();
          throw;
      }
  }

  /// <summary> Disposes the plugin and its services, will call upon the service collection so all services use their dispose function. </summary>
  public void Dispose()
      => _services?.Dispose(); // Dispose of all services. (call all of their dispose functions)
}
