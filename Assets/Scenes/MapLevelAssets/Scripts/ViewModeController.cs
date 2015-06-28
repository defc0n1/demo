﻿using System;
using ActionStreetMap.Core;
using ActionStreetMap.Infrastructure.Reactive;
using Assets.Scripts;
using Assets.Scripts.Character;
using UnityEngine;
using UnityEngine.UI;
using RenderMode = ActionStreetMap.Core.RenderMode;

namespace Assets.Scenes.MapLevelAssets.Scripts
{
    public class ViewModeController: MonoBehaviour
    {
        public Camera CameraScene;

        public Button OverviewButton;
        public Button SceneButton;

        public GameObject Character;

        private ApplicationManager _appManager;

        void Start()
        {
            CameraScene.enabled = true;
            _appManager = ApplicationManager.Instance;

            OverviewButton.gameObject.SetActive(true);
            SceneButton.gameObject.SetActive(false);

            OverviewButton.onClick.AsObservable().Subscribe(_ => SwitchSceneMode(true));
            SceneButton.onClick.AsObservable().Subscribe(_ => SwitchSceneMode(false));
        }

        private void SwitchSceneMode(bool isToOverview)
        {
            CameraScene.orthographic = isToOverview;

            // disable MouseOrbit/ThirdPersonController script to prevent interference with animation
            if (isToOverview)
            {
                CameraScene.GetComponent<MouseOrbit>().enabled = false;
                Character.GetComponent<ThirdPersonController>().enabled = false;
            }

            // NOTE workarounds to keep overview north oriented
            Character.transform.rotation = Quaternion.Euler(0, 0, 0);
            CameraScene.transform.rotation = Quaternion.Euler(90, 0, 0);

            // setup animation
            var cameraAnimation = CameraScene.GetComponent<CameraAnimation>();
            cameraAnimation.Play(2, isToOverview);
            Observable.FromEvent<EventHandler>(
                g => OnFinishAnimation,
                h => cameraAnimation.Finished += h,
                h => cameraAnimation.Finished -= h)
                .Take(1)
                .Subscribe();

            OverviewButton.gameObject.SetActive(!isToOverview);
            SceneButton.gameObject.SetActive(isToOverview);
        }

        private void OnFinishAnimation(object sender, EventArgs args)
        {
            var viewportHeight = 1200f;
            var viewportWidth = 1200f;
            var isToOverview = CameraScene.orthographic;
            if (isToOverview)
            {
                viewportHeight = CameraScene.orthographicSize * 2;
                viewportWidth = CameraScene.aspect * viewportHeight;
            }
            CameraScene.GetComponent<OverviewModeBehaviour>().enabled = isToOverview;
            CameraScene.GetComponent<MouseOrbit>().enabled = !isToOverview;
            Character.GetComponent<ThirdPersonController>().enabled = !isToOverview;

            _appManager.SwitchMode(isToOverview ? RenderMode.Overview : RenderMode.Scene,
                new MapRectangle(0, 0, viewportWidth, viewportHeight));
            
            var position = Character.transform.position;
            _appManager.Move(new MapPoint(position.x, position.z, position.y));
        }
    }
}
