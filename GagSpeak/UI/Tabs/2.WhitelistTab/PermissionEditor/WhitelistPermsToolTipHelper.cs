using System;
using System.Collections.Generic;

namespace GagSpeak.UI.Tabs.WhitelistTab;
public partial class WhitelistPanel {
    private Dictionary<string, Func<string>> tooltips;

    private void InitializeToolTips() {
        tooltips = new Dictionary<string, Func<string>>
        {
            // general settings
            ["usedSafewordTT"] = () => _activePanelTab == WhitelistPanelTab.TheirSettings
                ? $"If {_tempWhitelistChar._name.Split(' ')[0]} has recently used their safeword, and their grace period is still active."
                : "If you have already used your safeword and are on cooldown until you can use GagSpeak features again.",
            ["ExtendedLockTimesTT"] = () => _activePanelTab == WhitelistPanelTab.TheirSettings
                ? $"if {_tempWhitelistChar._name.Split(' ')[0]} is allowing you to lock their padlocks for longer than 12 hours."
                : $"If you are allowing {_tempWhitelistChar._name.Split(' ')[0]} to lock your padlocks for longer than 12 hours.",
            ["LiveChatGarblerTT"] = () => _activePanelTab == WhitelistPanelTab.TheirSettings
                ? $"If {_tempWhitelistChar._name.Split(' ')[0]} currently has their direct chat garbler enabled.\n"+
                "If this is enabled, it means anything they say in their enabled channels are converted to GagSpeak while gagged."
                : "If you currently have your direct chat garbler enabled.",
            ["LiveChatGarblerLockTT"] = () => _activePanelTab == WhitelistPanelTab.TheirSettings
                ? $"If {_tempWhitelistChar._name.Split(' ')[0]} currently has their direct chat garbler locked.\n"+
                "If they have this locked, it means nobody can disable their direct chat garbler except for the person who locked it."
                : "If you currently have your direct chat garbler locked.",
            ["WardrobeEnabledTT"] = () => _activePanelTab == WhitelistPanelTab.TheirSettings
                ? $"If {_tempWhitelistChar._name.Split(' ')[0]} has checked [Enable Wardrobe] in their config settings."
                : "If you have checked off [Enable Wardrobe] in your config settings.",
            ["PuppeteerEnabledTT"] = () => _activePanelTab == WhitelistPanelTab.TheirSettings
                ? $"If {_tempWhitelistChar._name.Split(' ')[0]} has checked [Enable Puppeteer] in their config settings."
                : "If you have checked off [Enable Puppeteer] in your config settings.",
            ["ToyboxEnabledTT"] = () => _activePanelTab == WhitelistPanelTab.TheirSettings
                ? $"If {_tempWhitelistChar._name.Split(' ')[0]} has checked [Enable Toybox] in their config settings."
                : "If you have checked off [Enable Toybox] in your config settings.",
            // gag interactions
            ["GagLayerSelectionTT"] = () => "Selects Which Gag Layer you are trying to interact with",
            ["GagTypeSelectionTT"] = () => $"Used to pick which type of gag you want to apply to {_tempWhitelistChar._name.Split(' ')[0]}",
            ["GagPadlockSelectionTT"] = () => $"Used to pick which padlock type you want to apply to {_tempWhitelistChar._name.Split(' ')[0]}.\n"+
            $"To Unlock one of {_tempWhitelistChar._name.Split(' ')[0]}'s padlocks, you must have the current padlock locked onto them selected.\n"+
            "(EX. If they currently have a combination padlock locked onto them, you need to have the combination padlock selected to unlock it.)",
            ["ApplyGagTT"] = () => $"Applies the selected gag to {_tempWhitelistChar._name.Split(' ')[0]}",
            ["ApplyPadlockTT"] = () => $"Applies the selected padlock to {_tempWhitelistChar._name.Split(' ')[0]}",
            ["UnlockPadlockTT"] = () => $"Unlocks the selected padlock from {_tempWhitelistChar._name.Split(' ')[0]}",
            ["RemoveGagTT"] = () => $"Removes the selected gag from {_tempWhitelistChar._name.Split(' ')[0]}",
            ["RemoveAllGagsTT"] = () => $"Removes all gags from {_tempWhitelistChar._name.Split(' ')[0]}",
            // wardrobe tooltips
            ["LockGagStorageTT"] = () => _activePanelTab == WhitelistPanelTab.TheirSettings
                ? $"If {_tempWhitelistChar._name.Split(' ')[0]}'s GagStorage UI becomes locked when they are gagged."
                : "If your GagStorage UI becomes locked when you are gagged.",
            ["AllowTogglingRestraintSetsTT"] = () => _activePanelTab == WhitelistPanelTab.TheirSettings
                ? $"If {_tempWhitelistChar._name.Split(' ')[0]} has allowed you to toggle their restraint sets."
                : $"If wish to allow {_tempWhitelistChar._name.Split(' ')[0]} to toggle your restraint sets.",
            ["AllowLockingRestraintSetsTT"] = () => _activePanelTab == WhitelistPanelTab.TheirSettings
                ? $"If {_tempWhitelistChar._name.Split(' ')[0]} has allowed you to lock their restraint sets."
                : $"If wish to allowing {_tempWhitelistChar._name.Split(' ')[0]} to lock your restraint sets.",
            ["ToggleSetTT"] = () => $"If the textfield contains a valid restraint set in {_tempWhitelistChar._name.Split(' ')[0]}'s restraint set list, you can toggle it on and off.",
            ["LockSetTT"] = () => $"If the textfield contains a valid timer format, and a valid set is in the text field above,\n"+
                                  $"you can lock {_tempWhitelistChar._name.Split(' ')[0]}'s active restraint set.",
            ["UnlockSetTT"] = () => $"If this textfield contains a valid restraint set, you can attempt to unlock {_tempWhitelistChar._name.Split(' ')[0]}'s locked restraint set.",
            ["StoredSetListTT"] = () => $"Contains the full list of {_tempWhitelistChar._name.Split(' ')[0]}'s active restraint sets.\n"+
                $"To get this list of restraint sets, you will need to have {_tempWhitelistChar._name.Split(' ')[0]} send you their copied list,\n"+
                "Then click the handcuff icon on the bottom left of the whitelist tab to paste the set list in.",
            // puppetter tooltips
            ["AllowSitPermTT"] = () => _activePanelTab == WhitelistPanelTab.TheirSettings
                ? $"If {_tempWhitelistChar._name.Split(' ')[0]} allows you to make them execute /sit and /groundsit commands using their trigger phrase"
                : $"If you are giving {_tempWhitelistChar._name.Split(' ')[0]} access to make you execute /sit and /groundsit commands with your trigger phrase.",
            ["AllowMotionPermTT"] = () => _activePanelTab == WhitelistPanelTab.TheirSettings
                ? $"If {_tempWhitelistChar._name.Split(' ')[0]} allows you to force them to do emotes and expressions with their trigger phrase."
                : $"If you are giving {_tempWhitelistChar._name.Split(' ')[0]} access to make you execute emotes and expressions with your trigger phrase.",
            ["AllowAllCommandsPermTT"] = () => _activePanelTab == WhitelistPanelTab.TheirSettings
                ? $"If {_tempWhitelistChar._name.Split(' ')[0]} allows you to make them execute any command."
                : $"If you are giving {_tempWhitelistChar._name.Split(' ')[0]} access to make you execute any command with your trigger phrase.",
            ["TriggerPhraseTT"] = () => _activePanelTab == WhitelistPanelTab.TheirSettings
                ? $"The Phrase that {_tempWhitelistChar._name.Split(' ')[0]} has set for you.\n"+
                  $"If you say this trigger phrase in chat, they will execute everything after it in the message.\n"+
                  $"Optionally, you can surround the command after the trigger in their start & end chars."
                : $"The Trigger Phrase that you have set for {_tempWhitelistChar._name.Split(' ')[0]}.\n"+
                  $"If {_tempWhitelistChar._name.Split(' ')[0]} says this in chat in any enabled channels,\n"+
                  $"you will execute whatever comes after the trigger phrase,\n(or what is enclosed within the start and end brackets)",
            ["StartCharTT"] = () => _activePanelTab == WhitelistPanelTab.TheirSettings
                ? $"The Start Character that {_tempWhitelistChar._name.Split(' ')[0]} has defined as the left enclosing bracket character.\n"+
                  $"Replaces the [ ( ] in Ex: [ TriggerPhrase (commandToExecute) ]"
                : $"The Start Character that you have defined as the left enclosing bracket character for your trigger phrase.\n"+
                  "Replaces the [ ( ] in Ex: [ TriggerPhrase (commandToExecute) ]",
            ["EndCharTT"] = () => _activePanelTab == WhitelistPanelTab.TheirSettings
                ? $"The End Character that {_tempWhitelistChar._name.Split(' ')[0]} has defined as the right enclosing bracket character.\n"+
                  $"Replaces the [ ) ] in Ex: [ TriggerPhrase (commandToExecute) ]"
                : $"The End Character that you have defined as the right enclosing bracket character for your trigger phrase.\n"+
                  "Replaces the [ ) ] in Ex: [ TriggerPhrase (commandToExecute) ]",
            ["AliasInputTT"] = () => _activePanelTab == WhitelistPanelTab.TheirSettings
                ? $"When you say this input command, {_tempWhitelistChar._name.Split(' ')[0]} will take it in as an alias and replace it with the output command."
                : $"When {_tempWhitelistChar._name.Split(' ')[0]} says this as a part of the command for you to execute after your trigger phrase,\n"+
                  "You will replace it with the alias output command before executing it.",
            ["AliasOutputTT"] = () => _activePanelTab == WhitelistPanelTab.TheirSettings
                ? $"What {_tempWhitelistChar._name.Split(' ')[0]} will replace with the corrisponding input command if it is included in the command to execute"
                : $"What you will replace the alias input command with if it is included in the command to execute after your trigger phrase.",
            // toybox tooltips
            ["ToyboxStateTT"] = () => _activePanelTab == WhitelistPanelTab.TheirSettings
                ? $"If {_tempWhitelistChar._name.Split(' ')[0]} has allowed you to lock their toybox UI."
                : $"If you are giving {_tempWhitelistChar._name.Split(' ')[0]} access to lock your toybox UI if they please.",
            ["AllowChangeToyStateTT"] = () => _activePanelTab == WhitelistPanelTab.TheirSettings
                ? $"If {_tempWhitelistChar._name.Split(' ')[0]} has allowed you to change their actively connected toys state."
                : $"If you are allowing {_tempWhitelistChar._name.Split(' ')[0]} to change the state of your actively connected toys state.",
            ["CanControlIntensityTT"] = () => _activePanelTab == WhitelistPanelTab.TheirSettings
                ? $"If {_tempWhitelistChar._name.Split(' ')[0]} has allowed you to control the intensity of their actively connected toy."
                : $"If you are allowing {_tempWhitelistChar._name.Split(' ')[0]} to control the intensity of your actively connected toy.",
            ["CanExecutePatternsTT"] = () => _activePanelTab == WhitelistPanelTab.TheirSettings
                ? $"If {_tempWhitelistChar._name.Split(' ')[0]} has allowed you to execute patterns to their actively connected toy."
                : $"If you are allowing {_tempWhitelistChar._name.Split(' ')[0]} to execute patterns to your actively connected toy.",
            ["IntensityMeterTT"] = () => $"The current intensity of {_tempWhitelistChar._name.Split(' ')[0]}'s actively connected toy.\n"+
                "This scale is based on their stepsize.",
            ["PatternTT"] = () => $"The current pattern that you are going to make {_tempWhitelistChar._name.Split(' ')[0]}'s actively connected toy execute.",
            ["PatternListTT"] = () => $"The list of patterns that {_tempWhitelistChar._name.Split(' ')[0]}'s actively connected toy has available to execute.",
            // general tooltips
            ["CurrentStateTT"] = () => $"If the Permission is allowed / not allowed",
            ["ReqTierTT"] = () => _activePanelTab == WhitelistPanelTab.TheirSettings
                ? $"The required tier you must be in order to toggle {_tempWhitelistChar._name.Split(' ')[0]}'s permission for this setting."
                : $"Required tier {_tempWhitelistChar._name.Split(' ')[0]} needs to have to override your setting for this permission.\n"+
                "You are able to toggle this regardless of tier, because you are configuring your own permissions.",
            ["ToggleButtonTT"] = () =>  _activePanelTab == WhitelistPanelTab.TheirSettings
                ? $"Toggle {_tempWhitelistChar._name.Split(' ')[0]}'s permission"
                : $"Toggle this permission, switching its state.\n"+
                  $"If state is checked, {_tempWhitelistChar._name.Split(' ')[0]} has access to it, if it is X, they do not.",
        }; 
    }
}