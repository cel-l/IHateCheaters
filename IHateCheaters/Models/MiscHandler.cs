using GorillaLocomotion;
using UnityEngine;

namespace IHateCheaters.Models;

public class MiscHandler : MonoBehaviour
{
    public static Transform? RealRightController;
    public static Transform? RealLeftController;

    private void Start()
    {
        RealRightController = new GameObject("RealRightController").transform;
        RealLeftController = new GameObject("RealLeftController").transform;
    }

    private void LateUpdate()
    {
        RealRightController?.position =
            GTPlayer.Instance.RightHand.controllerTransform.TransformPoint(
                GTPlayer.Instance.RightHand.handOffset
            );

        RealLeftController?.position =
            GTPlayer.Instance.LeftHand.controllerTransform.TransformPoint(
                GTPlayer.Instance.LeftHand.handOffset
            );

        RealRightController?.rotation =
            GTPlayer.Instance.RightHand.controllerTransform.rotation *
            GTPlayer.Instance.RightHand.handRotOffset;

        RealLeftController?.rotation =
            GTPlayer.Instance.LeftHand.controllerTransform.rotation *
            GTPlayer.Instance.LeftHand.handRotOffset;
    }
}