using System;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace GunplayHUD
{
    // The vanilla player Attack FSM writes Bodypart.Damage before sending Damage.
    // Observe that write without changing the FSM, then measure its Health/Condition
    // result after the frame's damage events. Gunplay's custom replay writes variables
    // directly and reports through Api, so this observer cannot count those shots twice.
    internal static class VanillaHits
    {
        private sealed class Hit
        {
            public GameObject Target;
            public FsmFloat Value;
            public float Before, Nominal;
            public Vector3 Point;
            public bool Head, Part;
        }
        private static readonly List<Hit> Pending = new List<Hit>();

        public static void BeforeSetFsmFloat(SetFsmFloat __instance)
        {
            try
            {
                if (!Api.Enabled || __instance.fsmName == null || __instance.fsmName.Value != "Bodypart" ||
                    __instance.variableName == null || __instance.variableName.Value != "Damage") return;
                var fsm = __instance.Fsm;
                if (fsm == null || fsm.Name != "Attack" || fsm.GameObject == null) return;
                bool gun = false;
                foreach (var other in fsm.GameObject.GetComponents<PlayMakerFSM>())
                    if (other.FsmName == "Reload") { gun = true; break; }
                if (!gun) return;
                var target = fsm.GetOwnerDefaultTarget(__instance.gameObject);
                if (target == null) return;

                GameObject valueOwner;
                FsmFloat value = CreatureHealth(target.transform, out valueOwner);   // (1.2.1) a creature riding in a car is not the car
                bool part = false;
                if (value == null)
                {
                    value = Condition(target.transform, out valueOwner);
                    part = value != null;
                    if (!part)
                    {
                        valueOwner = target.transform.root.gameObject;
                        value = Health(valueOwner);
                    }
                }
                // Ragdolls and scenery have no creature Health: no marker or number.
                if (value == null) return;
                var point = fsm.Variables.FindFsmVector3("hitPoint");
                Vector3 at = point != null ? point.Value : target.transform.position;
                bool head = !part && target.transform.parent != null && HasBodypart(target);
                float nominal = __instance.setValue != null ? Mathf.Abs(__instance.setValue.Value) : 0f;
                foreach (var pending in Pending)
                    if (pending.Target == valueOwner && pending.Value == value)
                    { pending.Nominal += nominal; pending.Point = at; pending.Head |= head; return; }
                Pending.Add(new Hit { Target = valueOwner, Value = value, Before = value.Value,
                    Nominal = nominal, Point = at, Head = head, Part = part });
            }
            catch (Exception e) { Plugin.Log.LogError("Vanilla hit observation: " + e); }
        }

        private static FsmFloat Condition(Transform target, out GameObject owner)
        {
            for (var t = target; t != null; t = t.parent)
                foreach (var fsm in t.GetComponents<PlayMakerFSM>())
                    if (fsm.FsmName == "Condition")
                    {
                        var value = fsm.FsmVariables.FindFsmFloat("Condition");
                        if (value != null) { owner = t.gameObject; return value; }
                    }
            owner = null; return null;
        }
        // the nearest Health FSM on the way up from the hit object, never past a vehicle part or a car root (Apocapatrol seats its crews
        // under the car, so transform.root of a driver is the car)
        private static FsmFloat CreatureHealth(Transform target, out GameObject owner)
        {
            for (var t = target; t != null; t = t.parent)
            {
                if (t.CompareTag("vehPart") || t.CompareTag("vehPartRemoved") || t.GetComponent("VehicleController") != null) break;
                foreach (var fsm in t.GetComponents<PlayMakerFSM>())
                    if (fsm.FsmName == "Health")
                    {
                        var value = fsm.FsmVariables.FindFsmFloat("Health");
                        if (value != null) { owner = t.gameObject; return value; }
                    }
            }
            owner = null; return null;
        }
        private static FsmFloat Health(GameObject root)
        {
            foreach (var fsm in root.GetComponentsInChildren<PlayMakerFSM>())
                if (fsm.FsmName == "Health")
                {
                    var value = fsm.FsmVariables.FindFsmFloat("Health");
                    if (value != null) return value;
                }
            return null;
        }
        private static bool HasBodypart(GameObject target)
        {
            foreach (var fsm in target.GetComponents<PlayMakerFSM>()) if (fsm.FsmName == "Bodypart") return true;
            return false;
        }
        public static void Flush()
        {
            foreach (var hit in Pending)
            {
                if (hit.Target == null) continue;
                float damage = Mathf.Max(0f, hit.Before - hit.Value.Value);
                // The game's FSM may process its event on the next tick. Preserve
                // the gun's nominal damage rather than silently losing feedback.
                if (damage <= 0f) damage = hit.Nominal;
                if (hit.Part) Hud.PartHit(hit.Target, hit.Point, damage);
                else Hud.PlayerHit(hit.Target, hit.Point, damage, hit.Head);
            }
            Pending.Clear();
        }
        public static void Clear() { Pending.Clear(); }
    }
}
