using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using GagSpeak.CharacterData;
using GagSpeak.Hardcore;

namespace GagSpeak.ChatMessages;
/// <summary>
/// Used for checking messages send to the games chatbox, not meant for detouring or injection
/// Messages passed through here are scanned to see if they are encoded, for puppeteer, or include any hardcore features.
public class HardcoreMsgDetector
{
    public FFXIVClientStructs.FFXIV.Client.Game.Control.EmoteController EmoteController;
    private readonly GagSpeakConfig     _config;
    private readonly HardcoreManager    _hcManager; // hardcore manager
    private readonly CharacterHandler   _characterHandler; // character handler
    private readonly IClientState       _client; // client state
    private readonly ITargetManager     _targetManager; // target manager
    private readonly IObjectTable       _objectTable; // object table
    //private readonly HardcoreMediator _hardcoreMediator; // hardcore mediator
    
    public HardcoreMsgDetector(HardcoreManager hardcoreManager, CharacterHandler characterHandler,
    IClientState clientState, ITargetManager targetManager, IObjectTable objectTable, GagSpeakConfig config) {
        _hcManager = hardcoreManager;
        _config = config;
        _characterHandler = characterHandler;
        _client = clientState;
        _targetManager = targetManager;
        _objectTable = objectTable;
    }

    // we want to make sure that regardless of if this sends as true or false, that we perform the nessisary logic for it so long as the relative option is enabled.
    public bool IsValidMsgTrigger(string senderName, SeString chatmessage, XivChatType type, out SeString messageToSend) {
        // create the string that will be sent out
        messageToSend = new SeString();
        
        // before we process anything, let's first see if the message was from us
        if(senderName == _client.LocalPlayer!.Name.TextValue) {
            // if it was, try to parse out the chatmessage to see if it contains any of the hardcore commands such as "{name}, follow me." or "{name}, sit."
            // or "{name}, stay here until i return." or "you may stand now {name}." or "thank you for waiting, {name}."
            // if any of these are found, then we should extract the name value and perform logic for it
            List<string> names = _characterHandler.whitelistChars
                .Where(x => x._inHardcoreMode)
                .Select(x => x._name)
                .ToList();
            // detect if the message contains any of the hardcore commands
            foreach (string name in names) {
                // follow me
                if(chatmessage.TextValue.ToLowerInvariant().Contains($"{name.Split(' ')[0].ToLowerInvariant()}, follow me.")) {
                    // get the index of the name
                    int index = _characterHandler.GetWhitelistIndex(name);
                    // toggle that players forcedfollow to true
                    _characterHandler.whitelistChars[index]._forcedFollow = true;
                    return false; // return false to avoid processing anymore logic
                }
                // sit start
                if(chatmessage.TextValue.ToLowerInvariant().Contains($"{name.Split(' ')[0].ToLowerInvariant()}, sit.")) {
                    int index = _characterHandler.GetWhitelistIndex(name);
                    _characterHandler.whitelistChars[index]._forcedSit = true;
                    return false;
                }
                // set end
                if(chatmessage.TextValue.ToLowerInvariant().Contains($"{name.Split(' ')[0].ToLowerInvariant()}, stay here until i return.")) {
                    int index = _characterHandler.GetWhitelistIndex(name);
                    _characterHandler.whitelistChars[index]._forcedToStay = false;
                    return false;
                }
                // stay here start
                if(chatmessage.TextValue.ToLowerInvariant().Contains($"you may stand now {name.Split(' ')[0].ToLowerInvariant()}.")) {
                    int index = _characterHandler.GetWhitelistIndex(name);
                    _characterHandler.whitelistChars[index]._forcedToStay = true;
                    return false;
                }
                // stay here end
                if(chatmessage.TextValue.ToLowerInvariant().Contains($"thank you for waiting, {name.Split(' ')[0].ToLowerInvariant()}.")) {
                    int index = _characterHandler.GetWhitelistIndex(name);
                    _characterHandler.whitelistChars[index]._forcedToStay = false;
                    return false;
                }
            }
            return false;
        }

        // dont process anything else if not in hardcore mode.
        if(!_config.hardcoreMode) {
            return false;
        }

        // first, let us get the index of the sender name since we already know they are in our whitelist 
        int senderIdx = _characterHandler.GetWhitelistIndex(senderName);
        // set our object to scan as the object in the same index of the list
        GagSpeak.Log.Debug($"Sender Index: {senderIdx}");
        HC_PerPlayerConfig playerConfig = _hcManager._perPlayerConfigs[senderIdx];
        // check to see if the message even matched before performing logic
        if(chatmessage.TextValue.ToLowerInvariant().Contains($"{_client.LocalPlayer!.Name.ToString().Split(' ')[0].ToLowerInvariant()}, follow me.")) {
            // if allowed forced follow, then scan to see if the incoming message contains the phrase required for our forced follow
            if(playerConfig._allowForcedFollow) {
                // we should first make sure that the player is somewhere in our current object table
                if (TryGetPlayerFromObjectTable(senderName, out GameObject SenderObj)) {
                    // the player is valid, and they are targetable, and we have forced to follow set to false
                    if(playerConfig._forcedFollow == false && SenderObj != null && SenderObj.IsTargetable) {
                        // we meet all the conditions to perform our logic, so we should set forced to folloow to true, locking our movement
                        _hcManager.LastMovementTime = DateTimeOffset.Now;
                        _hcManager.SetForcedFollow(senderIdx, true);
                        // then we should target the player
                        SetTarget(SenderObj);
                        // then we should execute /follow
                        messageToSend = "follow <t>";
                        // it is a valid trigger, so return true
                        return true;
                    }
                }
            }
        }

        // if allowed forced sit, then scan to see if the incoming message contains the phrase required for our forced sit
        if(playerConfig._allowForcedSit) {
            // if the message is the type to enable sitting
            if(chatmessage.TextValue.ToLowerInvariant().Contains($"{_client.LocalPlayer!.Name.ToString().Split(' ')[0].ToLowerInvariant()}, sit.")) {
                // and we are not already forced to sit
                if(playerConfig._forcedSit == false) {
                    // then we should set forced to sit to true, locking our movement
                    _hcManager.SetForcedSit(senderIdx, true);
                    // then we should execute /sit
                    messageToSend = "sit";
                    // it is a valid trigger, so return true
                    return true;
                }
            }
            // otherwise, see if we the command is to end our forced sit
            else if(chatmessage.TextValue.ToLowerInvariant().Contains($"you may stand now {_client.LocalPlayer!.Name.ToString().Split(' ')[0].ToLowerInvariant()}.")) {
                // and we are already forced to sit
                if(playerConfig._forcedSit) {
                    // then we should set forced to sit to false, unlocking our movement
                    _hcManager.SetForcedSit(senderIdx, false);
                    // while we performed the logic, we dont want to execute any chat commands, so return false
                    return false;
                }
            }
        }

        // if allowed forced to stay, then scan to see if the incoming message contains the phrase required for our forced to stay
        if(playerConfig._allowForcedToStay) {
            // if the message contains the phrase to enable forced to stay
            if(chatmessage.TextValue.ToLowerInvariant().Contains($"{_client.LocalPlayer!.Name.ToString().Split(' ')[0].ToLowerInvariant()}, stay here until i return.")) {
                // and we are not already forced to stay
                if(playerConfig._forcedToStay == false) {
                    // then we should set forced to stay to true, locking our movement
                    _hcManager.SetForcedToStay(senderIdx, true);
                    // it is a valid trigger, but we only want the logic, so return false
                    return false;
                }
            }
            // if the message contains the phrase to end forced to stay
            else if(chatmessage.TextValue.ToLowerInvariant().Contains($"thank you for waiting, {_client.LocalPlayer!.Name.ToString().Split(' ')[0].ToLowerInvariant()}.")) {
                // and we are already forced to stay
                if(playerConfig._forcedToStay) {
                    // then we should set forced to stay to false, unlocking our movement
                    _hcManager.SetForcedToStay(senderIdx, false);
                    // it is a valid trigger, but we only want the logic, so return false
                    return false;
                }
            }
        }
        // if none of these were true, just return false
        return false;
    }

    private bool TryGetPlayerFromObjectTable(string senderName, out GameObject SenderObj) {
        foreach (var obj in _objectTable) {
            if (obj is PlayerCharacter pc && pc.Name.TextValue == senderName) {
                SenderObj = pc;
                return true;
            }
        }
        SenderObj = null;
        return false;
    }
    public void SetTarget(GameObject obj) {
        _targetManager.Target = obj;
    }
}