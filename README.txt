Google Calendar Reminder
by Adam Milazzo
http://www.adammil.net/blog/v136_Google_Calendar_Reminder.html

Basic installation and use:

1. Open the .zip file and unpack the files somewhere (e.g. a folder on the
   desktop). If you want to compile it yourself, get the source code from the
   'src' directory. Otherwise, get the binaries from the 'bin' directory.
2. Run GoogleCalendarReminder.exe. On the first run, it will ask you to connect
   to Google and choose the calendars you want to watch. You probably also want
   to check the "Run on Windows Startup" box. Also, the default alarm sound is
   pretty loud; you can choose a quieter one if you prefer.
3. The program will create an icon in the system notification area (system
   tray). By default, Windows will hide the icon in the "^" ("Show hidden
   icons") section. Feel free to move it out of there if you want. Right-click
   the icon to change program settings. In any case, if it's connected it will
   pop up a window when you have a reminder.
4. Let me know if you run into any problems.

Upgrading from earlier versions:

1. Stop Google Calendar Reminder if it's running by right-clicking the
   notification icon (bottom-right corner) and choosing Quit.
2. Open the .zip file and replace your binaries with the new ones from the .zip file.
3. Restart Google Calendar Reminder by running GoogleCalendarReminder.exe.

Usage tips:

* Right-click an event to view relevant actions (such as joining a Zoom
  meeting). Double-clicking an event will take you to its calendar page.
* Mouse over events to view basic information.
* You don't have to accept the snooze options from the list. You can type in your
  own if you follow the same format. For example, type "3.5 minutes" to snooze
  for 3.5 minutes. You can also abbreviate: minutes -> m or min, hours -> h, days
  -> d, weeks -> w. For example, type "2h" to snooze for 2 hours.
* Double-click the program icon (in the system notification area) to unsnooze
  all snoozed events and show the window again (if there are any snoozed
  events).
* If you use the "Default reminder time" feature (enabled by default), you
  should normally leave the "minutes" box blank. Then the program will use
  different defaults for normal events (30 minute reminder) and all-day events
  (1 day reminder). (Of course, any reminder settings you specify in Google
  Calendar will take precedence.)
