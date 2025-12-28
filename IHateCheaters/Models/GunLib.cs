using GorillaLocomotion;
using UnityEngine;
using UnityEngine.InputSystem;

// ReSharper disable Unity.PreferNonAllocApi

namespace IHateCheaters.Models;

public enum GunType
{
    Rope,
    Static,
    Straight,
}

public class GunLib
{
    private const int ConstraintIterations = 5;
    private const int NumPoints = 50;
    public static readonly GunType GunType = GunType.Straight;
    private readonly float gravity = Physics.gravity.magnitude;
    public VRRig? ChosenRig;
    private LineRenderer? gunLine;
    public RaycastHit Hit;
    public bool IsShooting;
    private Vector3[]? linePoints;
    private Vector3[]? previousPoints;
    public bool ShouldFollow = true;

    public void Start()
    {
        gunLine = new GameObject("GunLine").AddComponent<LineRenderer>();
        gunLine.positionCount = NumPoints;
        gunLine.useWorldSpace = true;
        gunLine.material = new Material(Shader.Find("GUI/Text Shader"));
        gunLine.gameObject.SetActive(false);
        linePoints = new Vector3[NumPoints];
        previousPoints = new Vector3[NumPoints];
        for (int i = 0; i < NumPoints; i++)
        {
            linePoints[i] = Vector3.zero;
            previousPoints[i] = Vector3.zero;
        }
    }

    public void OnDisable()
    {
        if (gunLine != null) gunLine.gameObject.SetActive(false);
    }

    public void LateUpdate()
    {
        var gripPressed = ControllerInputPoller.instance.rightControllerGripFloat > 0.7f &&
                          ControllerInputPoller.instance.rightControllerSecondaryButton;
        var triggerPressed = ControllerInputPoller.instance.rightControllerIndexFloat > 0.7f;
        if (gripPressed)
        {
            if (!MiscHandler.RealRightController) return;

            var realRightController = MiscHandler.RealRightController;
            var gunPosition = realRightController.position;
            var gunDirection = realRightController.forward;
            HandleShooting(new Ray(gunPosition, gunDirection), triggerPressed, gunPosition);
        }
        else if (Mouse.current.backButton.isPressed)
        {
            Camera cam = GorillaTagger.Instance.thirdPersonCamera.transform.GetChild(0).GetComponent<Camera>();
            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            HandleShooting(ray, Mouse.current.leftButton.isPressed, GTPlayer.Instance.bodyCollider.transform.position);
        }
        else
        {
            gunLine?.gameObject.SetActive(false);
            ChosenRig = null;
        }
    }

    private void HandleShooting(Ray ray, bool shooting, Vector3 fakeOrigin)
    {
        IsShooting = shooting;
        if (PhysicsRaycast(ray, out Hit, out var rig))
        {
            ChosenRig = rig;
            gunLine?.gameObject.SetActive(true);
            float time = Mathf.PingPong(Time.time, 1f);
            Color start = new Color32(68, 91, 173, 255);
            Color end = new Color32(93, 81, 207, 255);
            gunLine!.material.color = Color.Lerp(start, end, time);
            float scale = 0.0125f * GTPlayer.Instance.scale;
            gunLine.startWidth = scale;
            gunLine.endWidth = scale;
            Vector3 targetEndPos = Hit.point;
            if (IsShooting && ShouldFollow && ChosenRig) targetEndPos = ChosenRig.transform.position;
            if (!IsShooting) ChosenRig = null;
            HandleShootingVisuals(fakeOrigin, targetEndPos);
        }
        else
        {
            gunLine?.gameObject.SetActive(false);
            ChosenRig = null;
        }
    }

    private void HandleShootingVisuals(Vector3 origin, Vector3 end)
    {
        if (!IsShooting)
        {
            for (int i = 0; i < NumPoints; i++)
            {
                float t = i / (float)(NumPoints - 1);
                linePoints![i] = Vector3.Lerp(origin, end, t);
                previousPoints![i] = linePoints[i];
            }
        }
        else
        {
            switch (GunType)
            {
                case GunType.Rope:
                {
                    linePoints![0] = origin;
                    linePoints[NumPoints - 1] = end;
                    for (int i = 1; i < NumPoints - 1; i++)
                    {
                        Vector3 velocity = linePoints[i] - previousPoints![i];
                        previousPoints[i] = linePoints[i];
                        linePoints[i] += velocity;
                        linePoints[i] += Vector3.down * (gravity * Time.deltaTime * Time.deltaTime);
                    }

                    for (var iter = 0; iter < ConstraintIterations; iter++)
                    {
                        for (var i = 0; i < NumPoints - 1; i++)
                        {
                            Vector3 delta = linePoints[i + 1] - linePoints[i];
                            float dist = delta.magnitude;
                            float targetDist = Vector3.Distance(origin, end) / (NumPoints - 1);
                            Vector3 correction = delta.normalized * ((dist - targetDist) * 0.5f);
                            if (i != 0) linePoints[i] += correction;
                            if (i != NumPoints - 2) linePoints[i + 1] -= correction;
                        }
                    }

                    break;
                }
                case GunType.Static:
                {
                    for (var i = 0; i < NumPoints; i++)
                    {
                        previousPoints![i] = linePoints![i];
                        linePoints[i] = Vector3.Lerp(origin, end, i / (float)(NumPoints - 1));
                    }

                    break;
                }
                default:
                {
                    for (var i = 0; i < NumPoints; i++)
                    {
                        previousPoints![i] = linePoints![i];
                        linePoints[i] = Vector3.Lerp(origin, end, i / (float)(NumPoints - 1));
                    }

                    break;
                }
            }
        }

        for (var i = 0; i < NumPoints; i++) gunLine!.SetPosition(i, linePoints![i]);
    }

    private bool PhysicsRaycast(Ray ray, out RaycastHit hit, out VRRig? rig)
    {
        var hits = Physics.RaycastAll(ray, 1000f);
        hit = default;
        rig = null;
        var rigDist = float.MaxValue;
        var worldDist = float.MaxValue;
        RaycastHit worldHit = default;
        foreach (var h in hits)
        {
            var foundRig = h.collider.GetComponentInParent<VRRig>();
            if (foundRig && !foundRig.isLocal)
            {
                if (h.distance < rigDist)
                {
                    rigDist = h.distance;
                    rig = foundRig;
                    hit = h;
                }
            }
            else if ((GTPlayer.Instance.locomotionEnabledLayers & (1 << h.collider.gameObject.layer)) != 0)
            {
                if (h.distance < worldDist)
                {
                    worldDist = h.distance;
                    worldHit = h;
                }
            }
        }

        if (rig) return true;
        if (worldDist < float.MaxValue)
        {
            hit = worldHit;
            return true;
        }

        return false;
    }
}