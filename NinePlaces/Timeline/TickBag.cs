using System.Collections.Generic;

namespace NinePlaces
{
    public class TickBag
    {
        // we keep track of the parent so that we can call reconnect
        // on our ticks before we hand them out.
        ITickRibbon m_oParent = null;

        // this is a dictionoary of queues mapped from duration (eg month) to a queue
        // of month tickelements.
        Dictionary<TickElementDuration, Queue<ITickElement>> m_dictQueues = new Dictionary<TickElementDuration, Queue<ITickElement>>();

        public TickBag( ITickRibbon in_oParent )
        {
            m_oParent = in_oParent;
            m_dictQueues.Add(TickElementDuration.Month, new Queue<ITickElement>());
            m_dictQueues.Add(TickElementDuration.Day, new Queue<ITickElement>());
            m_dictQueues.Add(TickElementDuration.Hour, new Queue<ITickElement>());
        }

        public static int g_nEnqueues = 0;
        public static int g_nDequeues = 0;
        public static int g_nCreatedTicks = 0;
        public static int g_nDeletes = 0;
        public ITickElement GetTick(TickElementDuration in_oDuration)
        {
            // we're being asked for a tick.  if we don't have any, just return a new one!
            ITickElement iRet = null;
            if (m_dictQueues[in_oDuration].Count == 0)
            {
                g_nCreatedTicks++;
                return new TickElement(m_oParent, in_oDuration);
            }

            // we have a tick in our queue, so return it.
            iRet = m_dictQueues[in_oDuration].Dequeue();

            System.Diagnostics.Debug.Assert(m_oParent != null);
            //reconnect first!
            iRet.Reconnect(m_oParent);

            g_nDequeues++;
            return iRet;
        }

        public void GiveTick(ITickElement in_oTick)
        {
            // disconnect our tick from its external dependencies
            in_oTick.Disconnect();
            // and enequeue it - but only if we need it!
            if (m_dictQueues[in_oTick.TickDuration].Count < 500)
            {
                m_dictQueues[in_oTick.TickDuration].Enqueue(in_oTick);
                g_nEnqueues++;
                return;
            }

            g_nDeletes++;
        }
    }
}
