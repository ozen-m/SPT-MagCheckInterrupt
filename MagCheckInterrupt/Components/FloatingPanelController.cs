using System;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.CameraControl;
using EFT.UI;
using HarmonyLib;
using MagCheckInterrupt.Patches;
using MagCheckInterrupt.Utils;
using UnityEngine;

namespace MagCheckInterrupt.Components;

public class FloatingPanelController : MonoBehaviour
{
    private AmmoCountPanel _panel;
    private Transform _panelTransform;
    private Transform _magazineTransform;
    private Camera _camera;
    private Action<Task> _onFinish;

    public static FloatingPanelController Instance
    {
        get
        {
            if (field == null)
            {
                field = Create();
            }

            return field;
        }
    }

    public static FloatingPanelController Create()
    {
        var localPlayer = GamePlayerOwner.MyPlayer.gameObject;
        var controllerObject = new GameObject(nameof(FloatingPanelController));
        controllerObject.transform.SetParent(localPlayer.transform);

        var canvasObject = new GameObject(nameof(Canvas), typeof(RectTransform), typeof(Canvas));
        canvasObject.transform.SetParent(controllerObject.transform, false);

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = CameraManager.Instance._camera;

        var canvasTransform = canvas.transform;
        canvasTransform.localScale = Vector3.one * 0.000375f;

        var ammoPanel = Singleton<CommonUI>.Instance.EftBattleUIScreen._ammoCountPanel;
        var panel = Instantiate(ammoPanel, canvasTransform, false);

        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        panelRect.localScale = Vector3.one;

        var controller = controllerObject.AddComponent<FloatingPanelController>();
        controller._panelTransform = panelRect;
        controller._panel = panel;
        controller._camera = CameraManager.Instance._camera;
        controller._onFinish = controller.AnimationFinished; // Cache the action

        return controller;
    }

    public void LateUpdate()
    {
        _panelTransform.localScale = Vector3.one * ConfigUtil.Scale.Value;
        _panelTransform.SetPositionAndRotation(
            _magazineTransform.position
            + (_magazineTransform.right * ConfigUtil.OffsetPosX.Value)
            + (_magazineTransform.up * ConfigUtil.OffsetPosY.Value)
            + (_magazineTransform.forward * ConfigUtil.OffsetPosZ.Value),
            Quaternion.LookRotation(_magazineTransform.position - _camera.transform.position)
        );
    }

    public void ShowAmmoDetails(FirearmController controller)
    {
        var magazine = controller.GunBaseTransform.GetComponentInChildren<MagazineInHandsVisualController>();
        _magazineTransform = magazine.transform;

        var ammoDetails = AmmoDetailsPatch.GetLastAmmoDetails();
        var message = ammoDetails.FoldingMechanimWeapon
            ? AmmoCountPanel.GetAmmoCountByLevelForFoldingMechanismWeapon(ammoDetails.AmmoCount, ammoDetails.MaxAmmoCount)
            : AmmoCountPanel.GetAmmoCountByLevel(ammoDetails.AmmoCount, ammoDetails.MaxAmmoCount, ammoDetails.Mastering);

        gameObject.SetActive(true);
        Show(message, ammoDetails.Details);
    }

    public void Show(string message, string details = null)
    {
        _panel.ShowGameObject();
        if (_animationField(_panel) == null)
        {
            _animationField(_panel) = _panel.gameObject.GetComponent<BattleUIComponentAnimation>();
        }
        _panel._ammoCount.text = message;
        _panel._ammoDetails.gameObject.SetActive(details != null);
        _panel._ammoDetails.text = details;
        _ = _animationField(_panel).Show(false);
    }

    public void Hide()
    {
        if (_animationField(_panel) == null)
        {
            return;
        }
        _ = _animationField(_panel).Hide().ContinueWith(_onFinish, TaskScheduler.Current);
    }

    private void AnimationFinished(Task _)
    {
        gameObject.SetActive(false);
        _magazineTransform = null;
    }

    private static readonly AccessTools.FieldRef<AmmoCountPanel, BattleUIComponentAnimation> _animationField =
        AccessTools.FieldRefAccess<AmmoCountPanel, BattleUIComponentAnimation>("_animation");
}
