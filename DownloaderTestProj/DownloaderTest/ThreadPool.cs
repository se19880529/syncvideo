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

    class Worker
    {
        public ThreadTask worker;
        public long id; 
        public long priority = 0;
        public List<TaskContext> Jobs = new List<TaskContext>();
        List<TaskContext> ExecutingJobs = new List<TaskContext>();
        public bool HasJob()
        {
            return Jobs.Count > 0;
        }
        public bool HasUnfinishedJob()
        {
            return Jobs.Count > 0 || ExecutingJobs.Count > 0 ;
        }
        public void AddJob(TaskContext job)
        {
            if (job == null)
            {
                worker.GetHashCode();
            }
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
        public void DoAJob()
        {
            if (Jobs.Count > 0)
            {
                TaskContext job = Jobs[0];
                Jobs.RemoveAt(0);
                ExecutingJobs.Add(job);
                worker.DoWork(job);
                ExecutingJobs.Remove(job);
            }
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

        public bool HasUnfinishedJob(long workerid)
        {
            LinkedListNode<Worker> worker = null;
            if (_workerList.TryGetValue(workerid, out worker))
            {
                return worker.Value.HasUnfinishedJob();
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

        public void Start()
        {
            _destThreadCount = 100;
            _PowerThePool();
        }

        public void Stop()
        {
            _destThreadCount = 0;
        }

        public bool IsStoped()
        {
            return _threadCount == 0;
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
            Worker work = new Worker() { worker = task, priority = 0, id = workid };
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
                    node.Value.DoAJob();
                    break;
                }
                node = node.Next;
            }
            return !nojob;
        }

        int _threadCount = 0;
        int _destThreadCount = 100;
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
                if (_threadCount > _destThreadCount)
                {
                    _threadCount--;
                    break;
                }
            }
        }

        #endregion
    }
}
