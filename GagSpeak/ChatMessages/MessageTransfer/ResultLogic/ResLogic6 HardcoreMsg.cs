using System.Threading.Tasks;

namespace GagSpeak.ChatMessages.MessageTransfer;
/// <summary> This class is used to handle the decoding of messages for the GagSpeak plugin. </summary>
public partial class ResultLogic {
    // decoder for if the whitelisted user is toggling your _enableToybox permission
    public bool ReslogicToggleBlindfold(DecodedMessageMediator decodedMessageMediator, ref bool isHandled) {
        // get playerName
        string playerName = decodedMessageMediator.GetPlayerName(decodedMessageMediator.assignerName);
        // see if they exist
        if(_characterHandler.IsPlayerInWhitelist(playerName)) {
            // get the dynamictier and index
            int index = _characterHandler.GetWhitelistIndex(playerName);
            // if you have hardcore mode enabled
            if(_config.hardcoreMode) {
                // toggle the blindfold state
                Task.Run(() => _hardcoreManager.SetBlindfolded(index, !_hardcoreManager._perPlayerConfigs[index]._blindfolded, playerName));
                GSLogger.LogType.Debug($"[Message ResLogic]: {playerName} has toggled your blindfold, enjoy the darkness~");
                return true;
            }
            else {
                GSLogger.LogType.Error($"[Message ResLogic]: {playerName} tried to toggle your blindfold, but you don't have hardcore mode enabled!");
                return false;
            }
        }
        else {
            GSLogger.LogType.Error($"[Message Decoder]: {playerName} is not in the whitelist");
            return false;
        }
    }
}