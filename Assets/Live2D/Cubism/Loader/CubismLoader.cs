using System;
using System.Collections.Generic;
using UnityEngine;

namespace Live2D.Cubism.Loader
{
    using Result = Texture2D;
    using Callback = Action<string, Texture2D>;

    public interface ICubsimLoader
    {
        void Load(string key, Callback onLoaded);
    }

    public static class CubismLoader
    {
        private readonly static Dictionary<string, Request> _requests = new Dictionary<string, Request>();

        private static ICubsimLoader _loader;

        public static void Initialize(ICubsimLoader loader)
            => _loader = loader ?? throw new ArgumentNullException(nameof(loader));

        private static bool Validate()
        {
            if (_loader != null)
                return true;

            Debug.LogError($"{nameof(CubismLoader)} has not been initialized yet. Please call {nameof(CubismLoader)}.{nameof(Initialize)} method before Cubism is used.");
            return false;
        }

        internal static void Load(string key, Callback onLoaded = null)
        {
            if (!Validate())
                return;

            var exist = _requests.ContainsKey(key) && _requests[key] != null;

            if (!exist)
            {
                _requests[key] = new Request(key);
            }

            _requests[key].Handle(onLoaded);

            if (!exist)
            {
                _loader.Load(key, OnLoaded);
            }
        }

        private static void OnLoaded(string key, Result result)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError("Key is null or empty.");
                return;
            }

            if (!result)
            {
                Debug.LogError($"Result with key={key} is null.");
                return;
            }

            if (!_requests.ContainsKey(key) || _requests[key] == null)
            {
                Debug.LogError($"No request has been registered by key={key}.");
                return;
            }

            _requests[key].Handle(result);
        }

        private class Callbacks : List<Callback> { }

        private class Request
        {
            public string key { get; }

            public Callbacks callbacks { get; } = new Callbacks();

            public Result result { get; private set; }

            public Request(string key)
            {
                this.key = key ?? throw new ArgumentNullException(nameof(key));
            }

            public void Handle(Callback callback)
            {
                if (callback == null)
                    return;

                if (this.result)
                    callback(this.key, this.result);
                else
                    this.callbacks.Add(callback);
            }

            public void Handle(Result result)
            {
                if (!result)
                {
                    Debug.LogException(new ArgumentNullException(nameof(result)));
                    return;
                }

                this.result = result;

                if (this.callbacks.Count <= 0)
                    return;

                var callbacks = this.callbacks.ToArray();
                this.callbacks.Clear();

                for (var i = 0; i < callbacks.Length; i++)
                {
                    callbacks[i]?.Invoke(this.key, this.result);
                }
            }
        }
    }
}
