using System;
using System.Linq;
using Dalamud.Plugin.Services;
using System.Collections.Generic;
using Dalamud.Game.Text.SeStringHandling;
using OtterGui.Classes;
using GagSpeak.UI.GagListings;
using System.Text.RegularExpressions;

using Dalamud.Game.Text.SeStringHandling.Payloads;
using GagSpeak.Services;
using GagSpeak.UI.Helpers;
namespace GagSpeak.Chat.MsgResultLogic;

#pragma warning disable IDE1006
public class MessageResultLogic { // Purpose of class : To perform logic on client based on the type of the sucessfully decoded message.
    
    private GagListingsDrawer _gagListingsDrawer;
    private readonly IChatGui _clientChat;
    private readonly GagSpeakConfig _config;
    private readonly IClientState _clientState;
    private readonly GagAndLockManager _lockManager;

    public MessageResultLogic(GagListingsDrawer gagListingsDrawer, IChatGui clientChat, GagSpeakConfig config,
    IClientState clientState, GagAndLockManager lockManager) {
        _gagListingsDrawer = gagListingsDrawer;
        _clientChat = clientChat;
        _config = config;
        _clientState = clientState;
        _lockManager = lockManager;
    }
    
    public bool CommandMsgResLogic(string receivedMessage, List<string> decodedMessage, bool isHandled,
            IChatGui clientChat, GagSpeakConfig config)
    {
        var commandType = decodedMessage[0].ToLowerInvariant();
        var _ = commandType switch
        {
            "lock"              => HandleLockMessage(ref decodedMessage, ref isHandled, config),
            "lockpassword"      => HandleLockMessage(ref decodedMessage, ref isHandled, config),
            "locktimerpassword" => HandleLockMessage(ref decodedMessage, ref isHandled, config),
            "unlock"            => HandleUnlockMessage(ref decodedMessage, ref isHandled, config),
            "unlockpassword"    => HandleUnlockMessage(ref decodedMessage, ref isHandled, config),
            "removeall"         => HandleRemoveAllMessage(ref decodedMessage, ref isHandled, config),
            "remove"            => HandleRemoveMessage(ref decodedMessage, ref isHandled, config),
            "apply"             => HandleApplyMessage(ref decodedMessage, ref isHandled, config),
            _                => LogError("Invalid message parse, If you see this report it to cordy ASAP.")
        };
        return true;
    }

    // another function nearly identical to the above, but for handling whitelist messages. These dont take as much processing.
    public bool WhitelistMsgResLogic(string recieved, List<string> decodedMessage, bool isHandled,
            IChatGui clientChat, GagSpeakConfig config)
    {
        var commandType = decodedMessage[0].ToLowerInvariant();
        var _ = commandType switch
        {
            "requestmistressrelation" => HandleRequestMistressMessage(ref decodedMessage, ref isHandled, config),
            "requestpetrelation"      => HandleRequestPetMessage(ref decodedMessage, ref isHandled, config),
            "requestslaverelation"    => HandleRequestSlaveMessage(ref decodedMessage, ref isHandled, config),
            "removeplayerrelation"    => HandleRelationRemovalMessage(ref decodedMessage, ref isHandled, config),
            "orderforcegarblelock"    => HandleLiveChatGarblerLockMessage(ref decodedMessage, ref isHandled, config),
            "requestinfo"             => HandleInformationRequestMessage(ref decodedMessage, ref isHandled, config),
            "acceptmistressrelation"  => HandleAcceptMistressMessage(ref decodedMessage, ref isHandled, config),
            "acceptpetrelation"       => HandleAcceptPetMessage(ref decodedMessage, ref isHandled, config),
            "acceptslaverelation"     => HandleAcceptSlaveMessage(ref decodedMessage, ref isHandled, config),
            "provideinfo"             => HandleProvideInfoMessage(ref decodedMessage, ref isHandled, config),
            "provideinfo2"            => HandleProvideInfo2Message(ref decodedMessage, ref isHandled, config),
            _                         => LogError("Invalid message parse, If you see this report it to cordy ASAP.")
        };
        return true;
    }

    bool LogError(string errorMessage) { // error log helper function
        GagSpeak.Log.Debug(errorMessage);
        _clientChat.PrintError(errorMessage);
        return false;
    }

    // handle the lock message [commandtype, layer, gagtype/locktype, password, player, password2]
    private bool HandleLockMessage(ref List<string> decodedMessage, ref bool isHandled, GagSpeakConfig _config) {
        // first, check if we have valid layer
        if (decodedMessage[1] == "first") { decodedMessage[1] = "1"; } else if (decodedMessage[1] == "second") { decodedMessage[1] = "2"; } else if (decodedMessage[1] == "third") { decodedMessage[1] = "3"; }
        if (!int.TryParse(decodedMessage[1], out int layer)) { 
            isHandled = true; return LogError("[MsgResultLogic]: Invalid layer value.");}
        // second, make sure already have a gag on
        if (_config.selectedGagTypes[layer-1] == "None") {
            isHandled = true; return LogError($"[MsgResultLogic]: No gag applied for layer {layer}, cannot apply lock!");}
        // third, make sure we dont already have a lock here
        if (_config.selectedGagPadlocks[layer-1] != GagPadlocks.None) {
            isHandled = true; return LogError($"[MsgResultLogic]: Already a lock applied to gag layer {layer}!");}
        // all preconditions met, so now we can try to lock it.
        if (Enum.TryParse(decodedMessage[2], out GagPadlocks parsedLockType)) {
            // get our payload
            PlayerPayload playerPayload = _lockManager.GetPlayerPayload();
            string[] nameParts = decodedMessage[4].Split(' ');
            decodedMessage[4] = nameParts[0] + " " + nameParts[1];
            // if the lock type is a mistress padlock, make sure the assigner is a mistress
            _config._padlockIdentifier[layer-1].SetType(parsedLockType); // set the type of the padlock
            _lockManager.Lock((layer-1), decodedMessage[4], decodedMessage[3], decodedMessage[5], playerPayload.PlayerName);
            // if we reached this point, it means we sucessfully locked the layer
            _clientChat.Print(new SeStringBuilder().AddItalicsOn().AddYellow($"[GagSpeak]").AddText($"{playerPayload.PlayerName} locked your " +
            $"{_config.selectedGagTypes[layer-1]} with a {_config.selectedGagPadlocks[layer-1]}.").AddItalicsOff().BuiltString);
            GagSpeak.Log.Debug($"[MsgResultLogic]: Sucessful Logic Parse for /gag lock");
            return true; // sucessful parse
        } else {
            isHandled = true; return LogError("[MsgResultLogic]: Invalid /gag lock parameters sent in!");
        }
    }

    // handle the unlock message
    private bool HandleUnlockMessage(ref List<string> decodedMessage, ref bool isHandled, GagSpeakConfig _config) {
        // see if our layer is a valid layer
        if (decodedMessage[1] == "first") { decodedMessage[1] = "1"; } else if (decodedMessage[1] == "second") { decodedMessage[1] = "2"; } else if (decodedMessage[1] == "third") { decodedMessage[1] = "3"; }
        if (!int.TryParse(decodedMessage[1], out int layer)) { 
            isHandled = true; return LogError("[MsgResultLogic]: Invalid layer value.");}
        // second, make sure we have a lock on
        if (_config.selectedGagPadlocks[layer-1] == GagPadlocks.None) {
            isHandled = true; return LogError($"[MsgResultLogic]: No lock applied for layer {layer}, cannot remove lock!");}
        // if we made it here, we can try to unlock it.
        PlayerPayload playerPayload = _lockManager.GetPlayerPayload();
        string[] nameParts = decodedMessage[4].Split(' ');
        decodedMessage[4] = nameParts[0] + " " + nameParts[1];
        // try to unlock it
        if(decodedMessage[4] == _config.selectedGagPadlocksAssigner[layer-1]) {
            _lockManager.Unlock((layer-1), decodedMessage[4], decodedMessage[3], playerPayload.PlayerName); // attempt to unlock
            _clientChat.Print(new SeStringBuilder().AddItalicsOn().AddYellow($"[GagSpeak]").AddText($"{decodedMessage[4]} " +
            $"sucessfully unlocked the {_config.selectedGagPadlocks[layer-1]} from your {_config.selectedGagPadlocks}.").AddItalicsOff().BuiltString);        
            GagSpeak.Log.Debug($"[MsgResultLogic]: Sucessful Logic Parse for /gag unlock");
            return true; // sucessful parse
        } else {
            isHandled = true; return LogError($"[MsgResultLogic]: {decodedMessage[4]} is not the assigner of the lock on layer {layer}!");
        }
    }

    // handle the remove message
    private bool HandleRemoveMessage(ref List<string> decodedMessage, ref bool isHandled, GagSpeakConfig _config) {
        // first, see if our layer is a valid layer
        if (decodedMessage[1] == "first") { decodedMessage[1] = "1"; } else if (decodedMessage[1] == "second") { decodedMessage[1] = "2"; } else if (decodedMessage[1] == "third") { decodedMessage[1] = "3"; }
        if (!int.TryParse(decodedMessage[1], out int layer)) { 
            isHandled = true; return LogError("[MsgResultLogic]: Invalid layer value.");}
        // second, make sure that this layer has a gag on it
        if (_config.selectedGagTypes[layer-1] == "None") {
            isHandled = true; return LogError($"[MsgResultLogic]: There is no gag applied for gag layer {layer}, so no gag can be removed.");}
        // third, make sure there is no lock on that gags layer
        if (_config.selectedGagPadlocks[layer-1] != GagPadlocks.None) {
            isHandled = true; return LogError($"[MsgResultLogic]: There is a lock applied for gag layer {layer}, cannot remove gag!");}
        // finally, if we made it here, we can remove the gag
        string playerNameWorld = decodedMessage[4];
        string[] parts = playerNameWorld.Split(' ');
        string playerName = string.Join(" ", parts.Take(parts.Length - 1));
        _clientChat.Print(new SeStringBuilder().AddItalicsOn().AddYellow($"[GagSpeak]").AddText($"{playerName} removed your {_config.selectedGagTypes[layer-1]}, how sweet.").AddItalicsOff().BuiltString);
        _lockManager.RemoveGag(layer-1); // remove the gag
        GagSpeak.Log.Debug($"[MsgResultLogic]: Sucessful Logic Parse for /gag remove");
        return true; // sucessful parse
    }

    // handle the removeall message
    private bool HandleRemoveAllMessage(ref List<string> decodedMessage, ref bool isHandled, GagSpeakConfig _config) {
        // make sure all of our gagpadlocks are none, if they are not, throw exception
        if (_config.selectedGagPadlocks.Any(padlock => padlock != GagPadlocks.None)) {
            isHandled = true; return LogError("[MsgResultLogic]: Cannot remove all gags while locks are on any of them.");}
        // if we made it here, we can remove them all
        string playerNameWorld = decodedMessage[4]; 
        string[] parts = playerNameWorld.Split(' ');
        string playerName = string.Join(" ", parts.Take(parts.Length - 1));
        _clientChat.Print(new SeStringBuilder().AddItalicsOn().AddYellow($"[GagSpeak]").AddText($"{playerName} has removed all of your gags.").AddItalicsOff().BuiltString);
        _lockManager.RemoveAllGags(); // remove all gags
        GagSpeak.Log.Debug($"[MsgResultLogic]: Sucessful Logic Parse for /gag removeall");
        return true; // sucessful parse
    }

    // handle the apply message
    private bool HandleApplyMessage(ref List<string> decodedMessage, ref bool isHandled, GagSpeakConfig _config) {
        // first, see if our layer is a valid layer
        if (decodedMessage[1] == "first") { decodedMessage[1] = "1"; } else if (decodedMessage[1] == "second") { decodedMessage[1] = "2"; } else if (decodedMessage[1] == "third") { decodedMessage[1] = "3"; }
        if (!int.TryParse(decodedMessage[1], out int layer)) { 
            isHandled = true; return LogError("[MsgResultLogic]: Invalid layer value.");}
        // secondly, see if our gagtype is in our list of gagtypes
        if (!_config.GagTypes.ContainsKey(decodedMessage[2]) && _config.selectedGagTypes[layer-1] != "None") {
            isHandled = true; return LogError("[MsgResultLogic]: Invalid gag type.");}
        // if we make it here, apply the gag
        _lockManager.ApplyGag(layer-1, decodedMessage[2]);
        // send sucessful message to chat
        string playerNameWorld = decodedMessage[4];
        string[] parts = playerNameWorld.Split(' ');
        string playerName = string.Join(" ", parts.Take(parts.Length - 1));
        _clientChat.Print(new SeStringBuilder().AddItalicsOn().AddYellow($"[GagSpeak]").AddText($"You've been gagged by {playerName} with a {_config.selectedGagTypes[layer-1]}!").AddItalicsOff().BuiltString);
        GagSpeak.Log.Debug($"[MsgResultLogic]: Sucessful Logic Parse for /gag apply");
        return true; // sucessful parse
    }

    // handle the request mistress message
    private bool HandleRequestMistressMessage(ref List<string> decodedMessage, ref bool isHandled, GagSpeakConfig _config) {
        try { // whole goal is just to apply the request update to the players whitelist
            string playerNameWorld = decodedMessage[4];
            string[] parts = playerNameWorld.Split(' ');
            string world = parts.Length > 1 ? parts.Last() : "";
            string playerName = string.Join(" ", parts.Take(parts.Length - 1));
            // locate player in whitelist
            var playerInWhitelist = _config.Whitelist.FirstOrDefault(x => x.name == playerName);
            // see if they exist
            if(playerInWhitelist != null) {
                playerInWhitelist.PendingRelationRequestFromPlayer = "Mistress"; // this means, they want to become YOUR mistress.
                // Notify the user that someone wishes to establish a relationship
                _clientChat.Print(new SeStringBuilder().AddItalicsOn().AddYellow($"[GagSpeak]").AddText($"Mistress relation request recieved from {playerName}. Accept or Decline via whitelist profile.").AddItalicsOff().BuiltString);
                GagSpeak.Log.Debug($"[MsgResultLogic]: Sucessful Logic Parse for a mistress relation request from {playerName}");
            }
        } catch {
            return LogError($"ERROR, Invalid requestMistress message parse.");
        }
        return true;
    }

    // handle the request pet message, will be exact same as mistress one.
    private bool HandleRequestPetMessage(ref List<string> decodedMessage, ref bool isHandled, GagSpeakConfig _config) {
        try { // whole goal is just to apply the request update to the players whitelist
            string playerNameWorld = decodedMessage[4];
            string[] parts = playerNameWorld.Split(' ');
            string world = parts.Length > 1 ? parts.Last() : "";
            string playerName = string.Join(" ", parts.Take(parts.Length - 1));
            // locate player in whitelist
            var playerInWhitelist = _config.Whitelist.FirstOrDefault(x => x.name == playerName);
            // see if they exist
            if(playerInWhitelist != null) {
                playerInWhitelist.PendingRelationRequestFromPlayer = "Pet"; // this means, they want to become YOUR pet.
                // Notify the user that someone wishes to establish a relationship
                _clientChat.Print(new SeStringBuilder().AddItalicsOn().AddYellow($"[GagSpeak]").AddText($"Pet relation request recieved from {playerName}. Accept or Decline via whitelist profile.").AddItalicsOff().BuiltString);
                GagSpeak.Log.Debug($"[MsgResultLogic]: Sucessful Logic Parse pet relation request from {playerName}: {playerInWhitelist.PendingRelationRequestFromPlayer}");
            }
        } catch {
            return LogError($"ERROR, Invalid request pet message parse.");
        }
        return true;
    }

    // handle the request slave message
    private bool HandleRequestSlaveMessage(ref List<string> decodedMessage, ref bool isHandled, GagSpeakConfig _config) {
        try { // whole goal is just to apply the request update to the players whitelist
            string playerNameWorld = decodedMessage[4];
            string[] parts = playerNameWorld.Split(' ');
            string world = parts.Length > 1 ? parts.Last() : "";
            string playerName = string.Join(" ", parts.Take(parts.Length - 1));
            // locate player in whitelist
            var playerInWhitelist = _config.Whitelist.FirstOrDefault(x => x.name == playerName);
            // see if they exist
            if(playerInWhitelist != null) {
                playerInWhitelist.PendingRelationRequestFromPlayer = "Slave"; // this means, they want to become YOUR slave.
                // Notify the user that someone wishes to establish a relationship
                _clientChat.Print(new SeStringBuilder().AddItalicsOn().AddYellow($"[GagSpeak]").AddText($"Slave relation request recieved from {playerName}. Accept or Decline via whitelist profile.").AddItalicsOff().BuiltString);
                GagSpeak.Log.Debug($"[MsgResultLogic]: Sucessful Logic Parse for a slave relation request from {playerName}");
            }
        } catch {
            return LogError($"ERROR, Invalid request pet message parse.");
        }
        return true;
    }

    // handle the relation removal message
    private bool HandleRelationRemovalMessage(ref List<string> decodedMessage, ref bool isHandled, GagSpeakConfig _config) {
        try { // whole goal is just to apply the request update to the players whitelist
            string playerNameWorld = decodedMessage[4];
            string[] parts = playerNameWorld.Split(' ');
            string world = parts.Length > 1 ? parts.Last() : "";
            string playerName = string.Join(" ", parts.Take(parts.Length - 1));
            // locate player in whitelist
            var playerInWhitelist = _config.Whitelist.FirstOrDefault(x => x.name == playerName);
            // see if they exist
            if(playerInWhitelist != null) {
                // set the pending relationship to none and relationship with that player to none
                playerInWhitelist.relationshipStatus = "None";
                playerInWhitelist.PendingRelationRequestFromPlayer = "None"; // remove any recieved relation establishments or requests.
                playerInWhitelist.PendingRelationRequestFromYou = "None"; // remove any relations you have sent out
                _clientChat.Print(new SeStringBuilder().AddItalicsOn().AddYellow($"[GagSpeak]").AddText($"Relation Status with {playerName} sucessfully removed.").AddItalicsOff().BuiltString);
                GagSpeak.Log.Debug($"[MsgResultLogic]: Sucessful Logic Parse for relation removal");
            }
        } catch {
            return LogError($"ERROR, Invalid relation removal message parse.");
        }
        return true;
    }

    // handle the live chat garbler lock message
    private bool HandleLiveChatGarblerLockMessage(ref List<string> decodedMessage, ref bool isHandled, GagSpeakConfig _config) {
        // locate the player in the whitelist matching the playername in the list
        string playerNameWorld = decodedMessage[4];
        string[] parts = playerNameWorld.Split(' ');
        string world = parts.Length > 1 ? parts.Last() : "";
        string playerName = string.Join(" ", parts.Take(parts.Length - 1));
        var playerInWhitelist = _config.Whitelist.FirstOrDefault(x => x.name == playerName);
        // see if they exist AND sure they have a mistress relation on your end
        if(playerInWhitelist != null && playerInWhitelist.relationshipStatus == "Mistress") {
            if(_config.LockDirectChatGarbler == false) {
                _config.DirectChatGarbler = true; _config.LockDirectChatGarbler = true;
                _clientChat.Print(new SeStringBuilder().AddItalicsOn().AddYellow($"[GagSpeak]").AddRed($"Your Mistress has decided you no longer have permission to speak clearly...").AddItalicsOff().BuiltString);
                GagSpeak.Log.Debug($"[MsgResultLogic]: Sucessful Logic Parse toggling livegarblerlock to ON for the slave.");
            }
            else {
                _config.DirectChatGarbler = false; _config.LockDirectChatGarbler = false;
                _clientChat.Print(new SeStringBuilder().AddItalicsOn().AddYellow($"[GagSpeak]").AddRed($"Your Mistress returns your permission to speak once more. How Generous...").AddItalicsOff().BuiltString);
                GagSpeak.Log.Debug($"[MsgResultLogic]: Sucessful Logic Parse toggling livegarblerlock to OFF for the slave.");
            }
        }
        else {
            return LogError($"ERROR, Invalid live chat garbler lock message parse.");
        }
        return true;
    }

    // handle the information request
    private bool HandleInformationRequestMessage(ref List<string> decodedMessage, ref bool isHandled, GagSpeakConfig _config) {
        // because this command spits out our information about ourselves, we need an extra layer of security, making SURE the person 
        // using this on us HAS TO BE inside of our whitelist.
        try { 
            string playerNameWorld = decodedMessage[4];
            string[] parts = playerNameWorld.Split(' ');
            string world = parts.Length > 1 ? parts.Last() : "";
            string playerName = string.Join(" ", parts.Take(parts.Length - 1));
            // locate player in whitelist
            var playerInWhitelist = _config.Whitelist.FirstOrDefault(x => x.name == playerName);
            // see if they exist
            if(playerInWhitelist != null) {
                // they are in our whitelist, so set our information sender to the players name.
                _config.SendInfoName = playerNameWorld;
                _clientChat.Print(new SeStringBuilder().AddItalicsOn().AddYellow($"[GagSpeak]").AddText($"Received info request from {playerName}. Providing Information in 4 seconds.").AddItalicsOff().BuiltString);
                GagSpeak.Log.Debug($"[MsgResultLogic]: Sucessful Logic Parse for recieving an information request message");
            }
        } catch {
            return LogError($"ERROR, Invalid information request message parse.");
        }
        return true;
    }

    // handle the accept mistress request
    private bool HandleAcceptMistressMessage(ref List<string> decodedMessage, ref bool isHandled, GagSpeakConfig _config) {
        try { // whole goal is just to apply the request update to the players whitelist
            string playerNameWorld = decodedMessage[4];
            string[] parts = playerNameWorld.Split(' ');
            string world = parts.Length > 1 ? parts.Last() : "";
            string playerName = string.Join(" ", parts.Take(parts.Length - 1));
            // locate player in whitelist
            var playerInWhitelist = _config.Whitelist.FirstOrDefault(x => x.name == playerName);
            // see if they exist
            if(playerInWhitelist != null) {
                // set the pending relationship to none and relationship with that player to none
                _clientChat.Print(new SeStringBuilder().AddItalicsOn().AddYellow($"[GagSpeak]").AddText($"You are now {playerName}'s mistress. Enjoy~.").AddItalicsOff().BuiltString);
                GagSpeak.Log.Debug($"[MsgResultLogic]: Sucessful Logic Parse for Accepting Mistress relation");
            }
        } catch {
            return LogError($"ERROR, Invalid accept mistress message parse.");
        }
        return true;
    }

    // handle the accept pet request
    private bool HandleAcceptPetMessage(ref List<string> decodedMessage, ref bool isHandled, GagSpeakConfig _config) {
        try { // whole goal is just to apply the request update to the players whitelist
            string playerNameWorld = decodedMessage[4];
            string[] parts = playerNameWorld.Split(' ');
            string world = parts.Length > 1 ? parts.Last() : "";
            string playerName = string.Join(" ", parts.Take(parts.Length - 1));
            // locate player in whitelist
            var playerInWhitelist = _config.Whitelist.FirstOrDefault(x => x.name == playerName);
            // see if they exist
            if(playerInWhitelist != null) {
                // set the pending relationship to none and relationship with that player to none
                _clientChat.Print(new SeStringBuilder().AddItalicsOn().AddYellow($"[GagSpeak]").AddText($"You are now {playerName}'s pet. Enjoy yourself~.").AddItalicsOff().BuiltString);
                GagSpeak.Log.Debug($"[MsgResultLogic]: Sucessful Logic Parse for Accepting Pet relation");
            }
        } catch {
            return LogError($"ERROR, Invalid accept pet message parse.");
        }
        return true;
    }

    // handle the accept slave request
    private bool HandleAcceptSlaveMessage(ref List<string> decodedMessage, ref bool isHandled, GagSpeakConfig _config) {
        try { // whole goal is just to apply the request update to the players whitelist
            string playerNameWorld = decodedMessage[4];
            string[] parts = playerNameWorld.Split(' ');
            string world = parts.Length > 1 ? parts.Last() : "";
            string playerName = string.Join(" ", parts.Take(parts.Length - 1));
            // locate player in whitelist
            var playerInWhitelist = _config.Whitelist.FirstOrDefault(x => x.name == playerName);
            // see if they exist
            if(playerInWhitelist != null) {
                // set the pending relationship to none and relationship with that player to none
                _clientChat.Print(new SeStringBuilder().AddItalicsOn().AddYellow($"[GagSpeak]").AddText($"You are now {playerName}'s slave, Be sure to Behave~.").AddItalicsOff().BuiltString);
                GagSpeak.Log.Debug($"[MsgResultLogic]: Sucessful Logic Parse for Accepting Slave relation");
            }
        } catch {
            return LogError($"ERROR, Invalid accept Slave message parse.");
        }
        return true;
    }

    private string playerNameTemp = "";
    // handle the provide information message
    private bool HandleProvideInfoMessage(ref List<string> decodedMessage, ref bool isHandled, GagSpeakConfig _config) {
        try {
            string playerNameWorld = decodedMessage[4];
            string[] parts = playerNameWorld.Split(' ');
            string world = parts.Length > 1 ? parts.Last() : "";
            string playerName = string.Join(" ", parts.Take(parts.Length - 1));
            // locate player in whitelist
            var playerInWhitelist = _config.Whitelist.FirstOrDefault(x => x.name == playerName);
            // see if they exist
            if(playerInWhitelist != null) {
                // set the pending relationship to none and relationship with that player to non
                playerInWhitelist.isDomMode = decodedMessage[1] == "true" ? true : false;
                playerInWhitelist.garbleLevel = int.Parse(decodedMessage[3]);
                playerInWhitelist.selectedGagTypes[0] = decodedMessage[6];
                playerInWhitelist.selectedGagTypes[1] = decodedMessage[7];
                playerInWhitelist.selectedGagPadlocks[0] = (GagPadlocks)Enum.Parse(typeof(GagPadlocks), decodedMessage[9]);
                playerInWhitelist.selectedGagPadlocks[1] = (GagPadlocks)Enum.Parse(typeof(GagPadlocks), decodedMessage[10]);
                playerInWhitelist.selectedGagPadlocksAssigner[0] = decodedMessage[12];
                playerInWhitelist.selectedGagPadlocksAssigner[1] = decodedMessage[13];
                playerInWhitelist.selectedGagPadlocksTimer[0] = UIHelpers.GetEndTime(decodedMessage[15]);
                playerInWhitelist.selectedGagPadlocksTimer[1] = UIHelpers.GetEndTime(decodedMessage[16]);

                GagSpeak.Log.Debug($"[MsgResultLogic]: Received information response from {playerName} [Part 1/2]");
                playerNameTemp = playerName; // transfer over to the 2nd function
            }            
        } catch {
            return LogError($"[MsgResultLogic]: Invalid provideInfo [1/2] message parse.");
        }        
        return true;
    }

    private bool HandleProvideInfo2Message(ref List<string> decodedMessage, ref bool isHandled, GagSpeakConfig _config) {
        try { 
            string playerName = playerNameTemp;
            // locate player in whitelist
            var playerInWhitelist = _config.Whitelist.FirstOrDefault(x => x.name == playerName);
            // see if they exist
            if(playerInWhitelist != null) {
                // set the pending relationship to none and relationship with that player to non
                playerInWhitelist.lockedLiveChatGarbler = decodedMessage[2] == "True" ? true : false;
                //playerInWhitelist.relationshipStatus = decodedMessage[5];
                playerInWhitelist.selectedGagTypes[2] = decodedMessage[8];
                playerInWhitelist.selectedGagPadlocks[2] = (GagPadlocks)Enum.Parse(typeof(GagPadlocks), decodedMessage[11]);
                playerInWhitelist.selectedGagPadlocksAssigner[2] = decodedMessage[14];
                playerInWhitelist.selectedGagPadlocksTimer[2] = UIHelpers.GetEndTime(decodedMessage[17]);
                
                _clientChat.Print(new SeStringBuilder().AddItalicsOn().AddYellow($"[GagSpeak]").AddText($"Finished Recieving Information from {playerName}.").AddItalicsOff().BuiltString);
                GagSpeak.Log.Debug($"[MsgResultLogic]: Received information response from {playerName} [Part 2/2]");
            }            
        } catch {
            return LogError($"[MsgResultLogic]: Invalid provideInfo [2/2] message parse.");
        }     
        return true;
    }
}
#pragma warning restore IDE1006