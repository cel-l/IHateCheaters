using GorillaLocomotion;
using UnityEngine;
using UnityEngine.Rendering;

namespace IHateCheaters.Models;

public class MiscHandler : MonoBehaviour
{
    public static Transform? RealRightController;
    public static Transform? RealLeftController;
    private static readonly int SrcBlend = Shader.PropertyToID("_SrcBlend");
    private static readonly int DstBlend = Shader.PropertyToID("_DstBlend");
    private static readonly int SrcBlendAlpha = Shader.PropertyToID("_SrcBlendAlpha");
    private static readonly int DstBlendAlpha = Shader.PropertyToID("_DstBlendAlpha");
    private static readonly int ZWrite = Shader.PropertyToID("_ZWrite");
    private static readonly int AlphaToMask = Shader.PropertyToID("_AlphaToMask");

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