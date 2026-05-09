using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dawnholder.Server.Network
{
    public interface IJobQueue
    {
        void Push(Action job);
    }

    public class JobQueue : IJobQueue
    {
        Queue<Action> _JobQueue = new Queue<Action>();
        object m_Lock = new object();
        bool m_Flush = false;

        public void Push(Action job)
        {
            bool flush = false;

            lock (m_Lock)
            {
                _JobQueue.Enqueue(job);

                if(m_Flush == false)
                {
                    flush = m_Flush = true;
                }
            }

            if (flush)
                Flush();
        }

        Action? Pop()
        {
            lock (m_Lock)
            {
                if (_JobQueue.Count == 0)
                {
                    m_Flush = false;
                    return null;
                }
                return _JobQueue.Dequeue();
            }
        }

        void Flush()
        {
            while (true)
            {
                Action? action = Pop();
                if (action == null)
                    return;

                action.Invoke();
            }
        }
    }
}
