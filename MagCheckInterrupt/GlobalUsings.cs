// ReSharper disable RedundantUsingDirective.Global

global using static EFT.Player;

// Inventory operations
global using IOneItemOperation = EFT.InventoryLogic.Operations.IOneItemOperation;
global using IInventoryOperation = EFT.InventoryLogic.Operations.IInventoryOperation;
global using ISubOperation = EFT.InventoryLogic.Operations.ISubOperation;
global using AddSuboperation = EFT.InventoryLogic.AddSuboperation;
global using RemoveSuboperation = EFT.InventoryLogic.RemoveSuboperation;
global using InsertMagResult = EFT.Player.FirearmController.InsertMagResult;
global using ReloadExternalMagResult = EFT.Player.FirearmController.ReloadExternalMagResult;

// Animation operations
global using ObjectInHandsOperation = EFT.Player.ObjectInHandsOperation;
global using FirearmOperation = EFT.Player.FirearmController.FirearmOperation;
global using ReloadExternalMagOperation = EFT.Player.FirearmController.ReloadExternalMagOperation;
global using Idling = EFT.Player.FirearmController.Idling;
global using UtilityOperation = EFT.Player.FirearmController.UtilityOperation;
global using InsertMagOperation = EFT.Player.FirearmController.InsertMagOperation;
global using PullOutMagOperation = EFT.Player.FirearmController.PullOutMagOperation;
global using Remove = EFT.Player.FirearmController.Remove;

// Others
global using UnityAnimatorWrapper = AnimationSystem.UnityAnimatorWrapper;
global using PlayerDebugSnapshotCreator = CommonAssets.Scripts.Utilities.PlayerDebugSnapshotCreator;
