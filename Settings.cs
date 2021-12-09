﻿namespace CalendarReminder
{
    enum OnCloseBehavior { Ask = 0, SnoozeAll = 1, DismissAll = 2 }

    static class Settings
    {
        public const string CalendarIds = "gcr.calendarIds";
        public const string DefaultReminder = "gcr.defaultReminder";
        public const string OnClose = "gcr.onClose";
        public const string PlaySound = "gcr.playSound";
    }
}
