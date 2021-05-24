// Copyright (c) 2020 by Rob Jellinghaus.

namespace Distributed.State
{
    /// <summary>
    /// Message sent by Peers that hear announcements which don't mention them.
    /// </summary>
    /// <remarks>
    /// Sent as a direct unconnected message to inform the newly arriving announcer of this peer's endpoint.
    /// Only listener peers send these messages, as only they hear the announcements in the first place.
    /// 
    /// Note that there is deliberately no payload at all!  This is because the only information needed by
    /// the announcer is the endpoint of the respondent, and the announcer will get that as part of the
    /// message transmission, so there is no need to duplicate it in the message payload.
    /// </remarks>
    public class AnnounceResponseMessage
    {
    }
}
