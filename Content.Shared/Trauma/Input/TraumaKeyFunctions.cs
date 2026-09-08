// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Input;

namespace Content.Trauma.Common.Input
{
    [KeyFunctions]
    public static class TraumaKeyFunctions
    {
        public static readonly BoundKeyFunction Strafe = "Strafe";

        // Targeting
        public static readonly BoundKeyFunction TargetingMod = "TargetingMod";
        public static readonly BoundKeyFunction TargetHead = "TargetHead";
        public static readonly BoundKeyFunction TargetChest = "TargetChest";
        public static readonly BoundKeyFunction TargetGroin = "TargetGroin";
        public static readonly BoundKeyFunction TargetLeftArm = "TargetLeftArm";
        public static readonly BoundKeyFunction TargetLeftHand = "TargetLeftHand";
        public static readonly BoundKeyFunction TargetRightArm = "TargetRightArm";
        public static readonly BoundKeyFunction TargetRightHand = "TargetRightHand";
        public static readonly BoundKeyFunction TargetLeftLeg = "TargetLeftLeg";
        public static readonly BoundKeyFunction TargetLeftFoot = "TargetLeftFoot";
        public static readonly BoundKeyFunction TargetRightLeg = "TargetRightLeg";
        public static readonly BoundKeyFunction TargetRightFoot = "TargetRightFoot";
    }
}

namespace Content.Shared.Trauma.Input
{
    public static class TraumaKeyFunctions
    {
        public static readonly BoundKeyFunction TargetingMod = Content.Trauma.Common.Input.TraumaKeyFunctions.TargetingMod;
        public static readonly BoundKeyFunction TargetHead = Content.Trauma.Common.Input.TraumaKeyFunctions.TargetHead;
        public static readonly BoundKeyFunction TargetChest = Content.Trauma.Common.Input.TraumaKeyFunctions.TargetChest;
        public static readonly BoundKeyFunction TargetGroin = Content.Trauma.Common.Input.TraumaKeyFunctions.TargetGroin;
        public static readonly BoundKeyFunction TargetLeftArm = Content.Trauma.Common.Input.TraumaKeyFunctions.TargetLeftArm;
        public static readonly BoundKeyFunction TargetLeftHand = Content.Trauma.Common.Input.TraumaKeyFunctions.TargetLeftHand;
        public static readonly BoundKeyFunction TargetRightArm = Content.Trauma.Common.Input.TraumaKeyFunctions.TargetRightArm;
        public static readonly BoundKeyFunction TargetRightHand = Content.Trauma.Common.Input.TraumaKeyFunctions.TargetRightHand;
        public static readonly BoundKeyFunction TargetLeftLeg = Content.Trauma.Common.Input.TraumaKeyFunctions.TargetLeftLeg;
        public static readonly BoundKeyFunction TargetLeftFoot = Content.Trauma.Common.Input.TraumaKeyFunctions.TargetLeftFoot;
        public static readonly BoundKeyFunction TargetRightLeg = Content.Trauma.Common.Input.TraumaKeyFunctions.TargetRightLeg;
        public static readonly BoundKeyFunction TargetRightFoot = Content.Trauma.Common.Input.TraumaKeyFunctions.TargetRightFoot;
    }
}
