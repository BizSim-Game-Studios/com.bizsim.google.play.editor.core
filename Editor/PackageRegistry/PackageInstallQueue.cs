using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

namespace BizSim.Google.Play.Editor.Core
{
    /// <summary>
    /// Sequential queue for UPM package installs.
    /// Enqueue one or more <see cref="InstallRequest"/>s, then call
    /// <see cref="ProcessNext"/> to start. Each install runs to completion
    /// before the next begins (Unity's PackageManager.Client only supports
    /// one concurrent add).
    /// </summary>
    public sealed class PackageInstallQueue
    {
        readonly Queue<InstallRequest> _queue = new();
        AddRequest _current;
        InstallRequest? _currentRequest;

        /// <summary>Fired after each install with the request and success flag.</summary>
        public event Action<InstallRequest, bool> OnItemCompleted;

        /// <summary>Fired when the queue is fully drained.</summary>
        public event Action OnAllCompleted;

        /// <summary>True while an install is in flight.</summary>
        public bool IsProcessing => _current != null;

        /// <summary>Number of queued (not yet started) installs.</summary>
        public int Remaining => _queue.Count;

        /// <summary>The package currently being installed, or null if idle.</summary>
        public InstallRequest? CurrentRequest => _currentRequest;

        /// <summary>Add a request to the end of the queue.</summary>
        public void Enqueue(InstallRequest request) => _queue.Enqueue(request);

        /// <summary>
        /// Start processing the queue. Dequeues the next item, calls
        /// Client.Add, and polls via EditorApplication.update.
        /// When each item completes, automatically proceeds to the next.
        /// </summary>
        public void ProcessNext()
        {
            if (_queue.Count == 0)
            {
                OnAllCompleted?.Invoke();
                return;
            }

            var req = _queue.Dequeue();
            _currentRequest = req;
            _current = Client.Add(req.InstallIdentifier);

            EditorApplication.CallbackFunction poll = null;
            poll = () =>
            {
                if (!_current.IsCompleted) return;

                EditorApplication.update -= poll;
                bool success = _current.Status == StatusCode.Success;
                _current = null;
                _currentRequest = null;

                OnItemCompleted?.Invoke(req, success);
                ProcessNext();
            };

            EditorApplication.update += poll;
        }
    }
}
