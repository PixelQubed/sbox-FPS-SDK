namespace Amper.Source1;

//
// Round
//

[EventDispatcherEvent] public class RoundRestartEvent : DispatchableEventBase { }
[EventDispatcherEvent] public class RoundActiveEvent : DispatchableEventBase { }
[EventDispatcherEvent] public class RoundEndEvent : DispatchableEventBase { }

//
// Game
//

[EventDispatcherEvent] public class GameRestartEvent : DispatchableEventBase { }
[EventDispatcherEvent] public class GameOverEvent : DispatchableEventBase { }

