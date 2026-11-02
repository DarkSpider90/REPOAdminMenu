using System;
using System.Collections.Generic;
using AdminMenu.Interop;
using UnityEngine;

namespace AdminMenu.Features;

/// <summary>
/// Lets the player at this machine fly through the level, ignoring the walls and the floor.
///
/// Holding a direction moves; letting go stops dead in mid-air rather than falling, which is
/// what makes it usable for looking around a level rather than just a slow jump. Up and down
/// are the jump and crouch keys, and sprint flies faster.
///
/// The first attempt at this set the body up once and left it alone, and the game quietly
/// took it back: gravity crept in and the player sank, the colliders on the avatar - a
/// different object from the controller - were never switched off so the floor still pushed
/// back, and after a few seconds of being in the air the game decided the player was falling
/// and curled them into a tumble with no way out.
///
/// So everything is re-asserted every frame instead of being set once. That is not thrift
/// lost: it is one rigidbody and three fields, against a game that is writing to the same
/// fields on its own schedule and will win any argument that is only had once.
/// </summary>
internal static class FreeFlight
{
    private const float Speed = 7f;
    private const float SprintSpeed = 18f;

    /// <summary>Kept topped up so the game never starts its own fall or tumble.</summary>
    private const float AntiGravitySeconds = 1f;

    private static readonly List<Collider> Disabled = new List<Collider>();

    private static bool _active;
    private static bool _savedGravity;
    private static bool _savedKinematic;
    private static bool _savedNoTumble;

    internal static bool IsActive => _active;

    internal static void SetActive(bool active)
    {
        if (_active == active)
        {
            return;
        }

        if (active)
        {
            _active = Start();
        }
        else
        {
            Stop();
            _active = false;
        }
    }

    /// <summary>Holds the player up and moves them. Called once a frame from the controller patch.</summary>
    internal static void Drive()
    {
        if (!_active)
        {
            return;
        }

        PlayerController controller = PlayerController.instance;
        if (controller == null || controller.rb == null)
        {
            SetActive(false);
            return;
        }

        HoldInPlace(controller);

        Vector3 direction = ReadDirection();
        if (direction == Vector3.zero)
        {
            return;
        }

        float speed = SemiFunc.InputHold(InputKey.Sprint) ? SprintSpeed : Speed;
        controller.transform.position += direction.normalized * (speed * Time.deltaTime);
    }

    /// <summary>
    /// Everything the game would otherwise undo. Re-applied every frame: the rigidbody so no
    /// force or gravity reaches it, the tumble flag so a long time off the ground is not read
    /// as a fall, and the velocity so letting go of the keys means stopping rather than
    /// drifting.
    /// </summary>
    private static void HoldInPlace(PlayerController controller)
    {
        controller.rb.isKinematic = true;
        controller.rb.useGravity = false;
        controller.rb.linearVelocity = Vector3.zero;
        controller.rb.angularVelocity = Vector3.zero;

        controller.DebugNoTumble = true;
        controller.AntiGravity(AntiGravitySeconds);

        // Colliders can come back with a new avatar, after a respawn for instance.
        DisableColliders();
    }

    private static Vector3 ReadDirection()
    {
        Vector3 direction = Vector3.zero;

        // Movement follows the camera, so "forward" means where the player is looking rather
        // than wherever the body happens to point.
        if (SemiFunc.PlayerCameraLocalGet(out Camera camera) && camera != null)
        {
            Transform view = camera.transform;
            direction += view.forward * SemiFunc.InputMovementY();
            direction += view.right * SemiFunc.InputMovementX();
        }

        if (SemiFunc.InputHold(InputKey.Jump))
        {
            direction += Vector3.up;
        }

        if (SemiFunc.InputHold(InputKey.Crouch))
        {
            direction += Vector3.down;
        }

        return direction;
    }

    private static bool Start()
    {
        PlayerController controller = PlayerController.instance;
        if (controller == null || controller.rb == null)
        {
            Plugin.Log.LogWarning("There is no player to fly yet.");
            return false;
        }

        try
        {
            _savedGravity = controller.rb.useGravity;
            _savedKinematic = controller.rb.isKinematic;
            _savedNoTumble = controller.DebugNoTumble;

            DisableColliders();

            Plugin.LogVerbose($"Free flight on; {Disabled.Count} colliders switched off.");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Could not start free flight. {ex}");
            Stop();
            return false;
        }
    }

    private static void Stop()
    {
        foreach (Collider collider in Disabled)
        {
            if (collider != null)
            {
                collider.enabled = true;
            }
        }

        Disabled.Clear();

        PlayerController controller = PlayerController.instance;
        if (controller?.rb == null)
        {
            return;
        }

        controller.rb.isKinematic = _savedKinematic;
        controller.rb.useGravity = _savedGravity;
        controller.DebugNoTumble = _savedNoTumble;
    }

    /// <summary>
    /// Switches off what the player bumps into. The colliders are spread over three objects -
    /// the controller, the collision helper and the avatar itself - and missing the avatar is
    /// why the first version still stood on the floor.
    /// </summary>
    private static void DisableColliders()
    {
        Collect(PlayerController.instance?.gameObject);
        Collect(PlayerCollision.instance?.gameObject);
        Collect(SemiFunc.PlayerAvatarLocal()?.gameObject);
    }

    private static void Collect(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
        {
            if (collider == null || !collider.enabled || collider.isTrigger)
            {
                // Triggers are left alone: they are how the game notices rooms, extraction
                // points and the truck, and switching them off would hide the flying player
                // from all of it.
                continue;
            }

            collider.enabled = false;
            Disabled.Add(collider);
        }
    }
}
