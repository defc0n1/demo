﻿using System;
using System.Collections;
using ActionStreetMap.Explorer.Commands;
using ActionStreetMap.Infrastructure.Reactive;
using ActionStreetMap.Unity.IO;
using Assets.Scripts.Console;
using Assets.Scripts.Console.Utils;
using Assets.Scripts.Demo;
using ActionStreetMap.Core;
using ActionStreetMap.Explorer;
using ActionStreetMap.Explorer.Bootstrappers;
using ActionStreetMap.Infrastructure.Bootstrap;
using ActionStreetMap.Infrastructure.Config;
using ActionStreetMap.Infrastructure.Dependencies;
using ActionStreetMap.Infrastructure.Diagnostic;
using ActionStreetMap.Infrastructure.IO;
using UnityEngine;
using Component = ActionStreetMap.Infrastructure.Dependencies.Component;

namespace Assets.Scripts.Character
{
    public class ActionStreetMapBehavior : MonoBehaviour
    {
        public float Delta = 10;

        private GameRunner _gameRunner;
        private IPositionObserver<MapPoint> _positionObserver;

        private DemoTileListener _messageListener;

        private ITrace _trace;

        private bool _isInitialized = false;

        private Vector3 _position = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        
        private DebugConsole _console;

        // Use this for initialization
        private void Start()
        {
            Initialize();
        }

        // Update is called once per frame
        private void Update()
        {
            if (_isInitialized && _position != transform.position)
            {
                _position = transform.position;
                Scheduler.ThreadPool.Schedule(() => 
                    _positionObserver.OnNext(new MapPoint(_position.x, _position.z, _position.y)));
            }
        }

        #region Initialization

        private void Initialize()
        {
            Scheduler.MainThread = new UnityMainThreadScheduler();
            UnityMainThreadDispatcher.RegisterUnhandledExceptionCallback(Debug.LogError);
            // create and register DebugConsole inside Container
            var container = new Container();
            var messageBus = new MessageBus();
            var pathResolver = new WinPathResolver();
            InitializeConsole(container);

            Scheduler.ThreadPool.Schedule(() =>
            {
                try
                {
                    var fileSystemService = new FileSystemService(pathResolver);
                    container.RegisterInstance(typeof (IPathResolver), pathResolver);
                    container.RegisterInstance(typeof (IFileSystemService), fileSystemService);
                    container.RegisterInstance<IConfigSection>(new ConfigSection(@"Config/settings.json", fileSystemService));

                    // actual boot service
                    container.Register(Component.For<IBootstrapperService>().Use<BootstrapperService>());

                    // boot plugins
                    container.Register(Component.For<IBootstrapperPlugin>().Use<InfrastructureBootstrapper>().Named("infrastructure"));
                    container.Register(Component.For<IBootstrapperPlugin>().Use<TileBootstrapper>().Named("tile"));
                    container.Register(Component.For<IBootstrapperPlugin>().Use<SceneBootstrapper>().Named("scene"));
                    container.Register(Component.For<IBootstrapperPlugin>().Use<DemoBootstrapper>().Named("demo"));

                    container.RegisterInstance(_trace);

                    // this class will listen messages about tile processing from ASM engine
                    _messageListener = new DemoTileListener(messageBus, _trace);

                    _gameRunner = new GameRunner(container, messageBus);
                    _positionObserver = _gameRunner;

                    _gameRunner.RunGame(new GeoCoordinate(55.7537315, 37.6198537));
                    _isInitialized = true;
                }
                catch (Exception ex)
                {
                    _console.LogMessage(new ConsoleMessage("Error running game:" + ex, RecordType.Error, Color.red));
                    throw;
                }
            });
        }

        private void InitializeConsole(IContainer container)
        {
            var consoleGameObject = new GameObject("_DebugConsole_");
            _console = consoleGameObject.AddComponent<DebugConsole>();
            container.RegisterInstance(_console);
            // that is not nice, but we need to use commands registered in DI with their dependencies
            _console.Container = container; 
            _trace = new DebugConsoleTrace(_console);
            _console.IsOpen = true;
        }

        #endregion
    }
}