using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace lib
{
    public class TaskContext
    {
    }
    public class ThreadTask
    {
        internal System.Action<TaskContext> task;
        public virtual void DoWork(TaskContext context)
        {
            task(context);
        }
    }

    private class Worker
    {
        public ThreadTask Worker;
        public long id; 
        public long priority = 0;
        public List<TaskContext> Jobs = new List<TaskContext>();
        public bool HasJob()
        {
            return Jobs.Count > 0;
        }
        public void AddJob(TaskContext job)
        {
            Jobs.Add(job);
        }
        public TaskContext PopJob()
        {
            if (Jobs.Count > 0)
            {
                TaskContext job = Jobs[0];
                Jobs.RemoveAt(0);
                return job;
            }
            return null;
        }
    }

    public class ThreadPool
    {
        public long AddWorker(ThreadTask task, long priority)
        {
            long id = _GetNextWorkId();
            var node = _InsertNewWorker(task, id);
            node.Value.priority = priority;
            _RefreshWorkerPriority(node);
            return id;
        }

        public bool HasJob(long workerid)
        {
            LinkedListNode<Worker> worker = null;
            if (_workerList.TryGetValue(workerid, out worker))
            {
                return worker.Value.HasJob();
            }
            else
                return false;
        }

        //return wheather worker exists
        public bool AddJob(TaskContext job, long workerid)
        {
            LinkedListNode<Worker> worker = null;
            if (_workerList.TryGetValue(workerid, out worker))
            {
                worker.Value.AddJob(job);
                return true;
            }
            else
                return false;
        }

        #region internal_implement

        long _idGenerator = 0;
        private long _GetNextWorkId()
        {
            return _idGenerator++;
        }

        Dictionary<long, LinkedListNode<Worker>> _workerList = new Dictionary<long, LinkedListNode<Worker>>();
        LinkedList<Worker> _workerPriorityList = new LinkedList<Worker>();
        LinkedListNode<Worker> _InsertNewWorker(ThreadTask task, long workid)
        {
            Worker work = new Worker() { Worker = task, priority = 0, id = workid };
            LinkedListNode<Worker> node = new LinkedListNode<Worker>(work);
            _workerList[workid] = node;
            _workerPriorityList.AddLast(node);
            return node;
        }
        void _RefreshWorkerPriority(LinkedListNode<Worker> worker)
        {
            while (worker != _workerPriorityList.First && worker.Previous.Value.priority <= worker.Value.priority)
            {
                var pre = worker.Previous;
                _workerPriorityList.Remove(worker);
                _workerPriorityList.AddBefore(pre, worker);
            }
            while (worker != _workerPriorityList.Last && worker.Next.Value.priority > worker.Value.priority)
            {
                var next = worker.Next;
                _workerPriorityList.Remove(worker);
                _workerPriorityList.AddAfter(next, worker);
            }
        }
        bool _DoOneJob()
        {
            bool nojob = true;
            LinkedListNode<Worker> node = _workerPriorityList.First;
            while (node != null)
            {
                if (node.Value.HasJob())
                {
                    var job = node.Value.PopJob();
                    node.Value.Worker.DoWork(job);
                    nojob = false;
                    break;
                }
                node = node.Next;
            }
            return !nojob;
        }

        int _threadCount = 0;
        int _destThreadCount = 10;
        //pool said: give me strength, i am xirui
        void _PowerThePool()
        {
            while (_threadCount < _destThreadCount)
            {
                var thread = new System.Threading.Thread(ThreadProc);
                thread.Start();
                _threadCount++;
            }
        }
        void ThreadProc()
        {
            while (true)
            {
                if (_DoOneJob())
                    System.Threading.Thread.Sleep(0);
                else
                    System.Threading.Thread.Sleep(10);
            }
        }

        #endregion
    }
}
