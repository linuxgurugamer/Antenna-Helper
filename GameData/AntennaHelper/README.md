# Antenna-Helper

Ever wonder what all the antennas you put on your craft, because they look so cool, actually do ? How far can you go with them ? How many science you'll be able to send back when you get there ?

It's easy ! Just multiply the power of your strongest antenna with the result of the division of the sum of all your antenna's power by the power of the strongest antenna raised by their average weighted combinability exponent. This will give you the antenna power of your spacecraft, now calculate the root square of this number multiplied by the DSN power, it will give you your maximum range. So simple.

**OR**

You could use Antenna Helper to do all that, and more, for you.



## Ok, but what does it do ?

It will show you the antenna capability of your active vessel in flight, in the editor and of **all** vessels in the tracking station.


## In flight ?

Click the Antenna Helper icon on the toolbar : ![Antenna Helper icon](https://github.com/Li0n-0/Antenna-Helper/blob/master/pics/icon_off.png?raw=true)
And you should see something close to that :

![flight main window](https://github.com/Li0n-0/Antenna-Helper/blob/master/pics/flight_main_window.png?raw=true)

The top line, in bold, show the signal strength of the whole CommNet path between your vessel and the DSN. Each button beneath it is a link in this path.

Clicking on the **Potential Relays** button will expand the window to show all the in-flight relays : 

![flight potential relays window](https://github.com/Li0n-0/Antenna-Helper/blob/master/pics/flight_potential_relays_window.png?raw=true)

You can click on every link to display some info about both vessel (or DSN) and about the link it-self : 

![flight link info window](https://github.com/Li0n-0/Antenna-Helper/blob/master/pics/flight_link_info_window.png?raw=true)

Now take a look at : 


## The Map-View

![map view active connect](https://github.com/Li0n-0/Antenna-Helper/blob/master/pics/map_view_active.png?raw=true)

A big circle has appeared ! Centered on the vessel or DSN your directly connected to (the first link of the main window), it helps visualize the range and signal decay of your active connection.
While your ship is in the green circle its signal strength will be clamped between 100 and 75%, in the yellow circle between 75 and 50%, in the orange circle between 50 and 25%, in the red circle between 25 and 0%. And if you're outside the red circle you don't have any connection.

With this new window (show it-self only in the map-view) :

![map view window](https://github.com/Li0n-0/Antenna-Helper/blob/master/pics/map_view_window.png?raw=true)

You can choose between different sources to show your potential range and signal strength : 

![map view patchwork](https://github.com/Li0n-0/Antenna-Helper/blob/master/pics/map_view_patchwork.png?raw=true)


## You mentioned the editor ?

Yep. That's nice to see "on live" where you can go with your space probe but it's even better to know before launch how far you can travel.

In the VAB (or SPH) click the Antenna Helper icon on the app launcher : ![Antenna Helper icon](https://github.com/Li0n-0/Antenna-Helper/blob/master/pics/icon_off.png?raw=true)
It brings this window :

![editor main window direct](https://github.com/Li0n-0/Antenna-Helper/blob/master/pics/editor_main_window_direct.png?raw=true)


Lots of info here, let's go through them from top to bottom :
* **Selected type :** which antennas are taking into account to compute the vessel antenna power. **Direct** (by default) will use all antennas, **Relay** will use only the antenna with relay capability.
* **Current target :** the range of your ship's antenna is determined by the antenna power of its target, which can be the DSN or a relay. By default the selected target is the DSN. You can change the target by clicking on **Pick A Target**. (more about it below)
* **Status :** a quick explanation about which antenna on your craft will actually be used in flight.
* **Power :** the antenna power of your vessel.
* **Max Range :** the maximum distance between your vessel and the target after which you'll lose the connection. Depend on the power of your vessel **and** the power of the target.
* **Max Distance At 100% :** the distance between your vessel and the target after which your signal strength will start to decay.
* **Color bar :** it works with the **Max Distance At 100%** and the **Max Range** distance. The number aligned with the black bars separating the colors indicate the distance at which the signal decay. Example from the pic above : between 2 050 205 945m and 51 662 800 363m, your signal strength will vary from 100% to 75%.


## So the target is important ?

Of course, one antenna on its own don't do anything. It must be connected to another antenna, can't compute maximum range or signal strength with only one antenna.
Two type of target :
* **the DSN (Deep Space Network) :** it's the (very) big antenna on Kerbin. It has three levels with different power, you upgrade from one to another by upgrading the Tracking Station.
* **relay(s) :** those are vessels you build yourself, two condition need to be meet : having at least one antenna with relay capability and setting the vessel type to "Relay".

You can simulate all those connection directly from the editor : Click the **Pick A Target** button : 

![editor target dsn window](https://github.com/Li0n-0/Antenna-Helper/blob/master/pics/editor_target_dsn_window.png?raw=true)


From here you can select a different DSN level, your current level is in **bold** and is selected by default. For simulating against in-flight relay hit the **In-Flight Ships** button :

![editor target flight window](https://github.com/Li0n-0/Antenna-Helper/blob/master/pics/editor_target_flight_window.png?raw=true)


In this window you should see one button per flying vessel with its name and its antenna **relay** power, important distinction, when your connection is going through a relay only the relay antenna of this relay will be used. Sound obvious but... So if your building a relay make sure your selected type, in the main window, is set to **RELAY**.

Speaking of building relays, you may want to simulate the antenna range of a relay before you launch it. To do so you need to add your ship/relay to the **Antenna Helper Editor Ship List**. Just open your vessel in the editor and click on **Add Ship to the Target List** in the main window. After which you can click on **Editor Ships** in the **Pick A Target** window : 

![editor target editor window](https://github.com/Li0n-0/Antenna-Helper/blob/master/pics/editor_target_editor_window.png?raw=true)


It works the same way as the in-flight relay list, the number between parenthesis is still the antenna **relay** power.



## But what all those numbers really mean ?

They are, mostly, just distance. At a solar system scale. So big distance, like space-travel distance (we're playing a space program game, are we not ?).
Anyway, to help figuring out what does numbers represent you'll find this window, by clicking on **Signal Strength / Distance** in the main window :

![editor signal per distance window](https://github.com/Li0n-0/Antenna-Helper/blob/master/pics/editor_signal_distance_window.png?raw=true)


In it you'll see what signal strength to expect for a distance equal to the minimum and maximum distance between the home body (Kerbin), its moon(s) and all the others planet on the solar system. Keep in mind that those distance are approximate, specially for celestial body with an highly inclined orbit. 
You can check the distance used by hovering your mouse on the celestial body name.
In the same window you can check the signal strength to expect at any given distance, write it in the input box at the bottom of the window and click the **Math !** button.


Still don't get a clear representation of what those space-travel distance mean ?
Save your ship in the **Antenna Helper Editor Ship List**, click the **Add Ship to the Target List** button in the main window. Quit the editor and open up the **Tracking Station**.



## Tracking Station ? Yummy !

Yes it's good. Fire Antenna Helper : ![Antenna Helper icon](https://github.com/Li0n-0/Antenna-Helper/blob/master/pics/icon_off.png?raw=true)

From there if you select a vessel in the Tracking Station list you'll see its range circles just like in flight. You can check the range for the active connection, the DSN connection and for all relay in flight by selecting it in the GUI.
To check the range of a vessel saved in the Editor click the **Editor Ship List** button.

![tracking station editor ship list window](https://github.com/Li0n-0/Antenna-Helper/blob/master/pics/tracking_station_editor_ship_list_window.png?raw=true)


Now you can see the range and the signal strength of your future vessel for the different connection type.
The number between parenthesis is the **total** antenna power of your vessel, as opposed to the Ship List in the Editor that show the **relay** power.

![tracking station editor ship](https://github.com/Li0n-0/Antenna-Helper/blob/master/pics/tracking_station_editor_ship.png?raw=true)



## Sounds awesome but I don't want/need X feature ?

In the **Advanced Settings** menu you'll find a tab for Antenna Helper. Here you can disable the mod per scene, choose to use [Blizzy/LinuxGuruGamer toolbar](https://forum.kerbalspaceprogram.com/index.php?/topic/161857-131-toolbar-continued-common-api-for-draggableresizable-buttons-toolbar/) instead of stock, and, set the refreshing speed for the flight UI, in case it slow your game.

![settings screen](https://github.com/Li0n-0/Antenna-Helper/blob/master/pics/settings_screen.png?raw=true)



## About the mod

**DOWNLOADS for KSP 1.4.0 :**
* [Get it with CKAN](https://forum.kerbalspaceprogram.com/index.php?/topic/154922-ckan-the-comprehensive-kerbal-archive-network-v1240-bruce/), thanks [linuxGuruGamer](https://forum.kerbalspaceprogram.com/index.php?/profile/129964-linuxgurugamer/)
* [SpaceDock](https://spacedock.info/mod/1730/Antenna%20Helper)
* [GitHub](https://github.com/Li0n-0/Antenna-Helper/releases)

**DEPENDENCY :**
* [Toolbar Controller](https://github.com/linuxgurugamer/ToolbarControl/releases)


**Previous KSP version :**
* [Last release for KSP 1.3.1](https://github.com/Li0n-0/Antenna-Helper/releases/tag/v1.0.0)

It is localized in Japanese, Simplified Chinese and Spanish.

It *should* be compatible with every mods, except RemoteTech.

If you find any bugs please report it, either on [GitHub](https://github.com/Li0n-0/Antenna-Helper/issues) or on this [KSP forum's thread](https://forum.kerbalspaceprogram.com/index.php?/topic/171900-131-antenna-helper-math-your-antenna-range-and-signal-strength-v100-9-mar-2018/).

[Know issues and *maybe* future plans](https://github.com/Li0n-0/Antenna-Helper/issues)

[Dev thread](https://forum.kerbalspaceprogram.com/index.php?/topic/156122-wip131-antenna-helper-looking-for-translators-v090-27-feb-2018/&do=findComment&comment=2947693) (closed), for posterity :wink: 


## Credits

The idea for an in-game calculator is from [this thread](https://forum.kerbalspaceprogram.com/index.php?/topic/153155-mod-idea-in-game-antenna-strength-calculater/) by [Tyko](https://forum.kerbalspaceprogram.com/index.php?/profile/164179-tyko/).

Thanks to [Poodmund](https://forum.kerbalspaceprogram.com/index.php?/profile/128643-poodmund/) for his [google doc's calculator](https://docs.google.com/spreadsheets/d/1qIgFB8OXnlgpPCGsxv7JYUYQq5O671IcZXpumVaStek/edit?usp=sharing), and for the help he provide to this mod.

Thanks to [Skalou](https://forum.kerbalspaceprogram.com/index.php?/profile/133496-skalou/) for his help with the math :wink:

Thanks to [wile1411](https://forum.kerbalspaceprogram.com/index.php?/profile/28891-wile1411/) for his bug report and suggestions.

Thanks to [Wyzard](https://forum.kerbalspaceprogram.com/index.php?/profile/162363-wyzard/) for his several bug fix.

Thanks to the translators : 
* Japanese version : [EBOSHI](https://forum.kerbalspaceprogram.com/index.php?/profile/165938-eboshi/), [COLOT](https://forum.kerbalspaceprogram.com/index.php?/profile/185886-colot/) and [anarog_1](https://forum.kerbalspaceprogram.com/index.php?/profile/172934-anarog_1/)
* Simplified Chinese : [CN_Warren](https://forum.kerbalspaceprogram.com/index.php?/profile/183380-cn_warren/)
* Spanish : [fitiales](https://forum.kerbalspaceprogram.com/index.php?/profile/66011-fitiales/)

Antenna Helper icon/logo made by myself with assets from [FlatIcon](https://www.flaticon.com) by [Freepik](https://www.flaticon.com/authors/freepik).

## [Thread on official KSP forum](https://forum.kerbalspaceprogram.com/index.php?/topic/171900-131-antenna-helper-math-your-antenna-range-and-signal-strength-v100-9-mar-2018/)
