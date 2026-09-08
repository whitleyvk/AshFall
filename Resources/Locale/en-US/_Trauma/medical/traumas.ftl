# SPDX-License-Identifier: AGPL-3.0-or-later

popup-trauma-BoneDamage-Damaged = You feel some pain in your {$part}.
popup-trauma-BoneDamage-Cracked = You feel a sharp pain in your {$part}!
popup-trauma-BoneDamage-Broken = You hear a loud crack in your {$part}!!
self-inspect-trauma-BoneDamage = hurts inside
self-inspect-trauma-BoneDamage-Large = feels dislocated
inspect-trauma-BoneDamage = looks dislocated

popup-trauma-OrganDamage-Damaged = Your {$part} feels very wrong...
popup-trauma-OrganDamage-Destroyed = You feel a very sharp pain in your {$part}!
self-inspect-trauma-OrganDamage = feels weird every time you breathe.

speech-needs-tongue = You cannot speak without a tongue!
eating-needs-organ-no-mouth-self = You have no mouth!
eating-needs-organ-no-mouth-other = {$target} has no mouth!

installable-organ-examine = You could [bold]use[/bold] this organ to install it into your body.
installable-organ-already-installed = You already have an organ in the {$organ} category!
installable-organ-installed = You install the {$organ} into your body.

equip-part-missing-error = {$target} is missing the required body part: {$part}.

reagent-effect-guidebook-adjust-bone-damage = Reduces bone damage by {$amount}

entity-condition-guidebook-has-organ = the target { $invert ->
    [true] does not have
   *[false] has
} an organ in the {$organ} category

entity-condition-guidebook-organ-slot = the target { $inverted ->
    [true] does not have
   *[false] has
} a {$slot} organ slot in its {$part}

entity-effect-guidebook-detach-part = { $chance ->
    [1] Detaches
   *[other] detach
} the body part from the body

entity-effect-guidebook-gib = { $chance ->
    [1] Gibs
   *[other] gib
} the target

entity-effect-guidebook-insert-new-organ = { $chance ->
    [1] Inserts
   *[other] insert
} a {$organ} into the target body part

entity-effect-guidebook-move-organ = { $chance ->
    [1] Moves
   *[other] move
} the {$organ} organ into {$dest}

entity-effect-guidebook-part-add-slot = { $chance ->
    [1] Adds
   *[other] add
} a {$slot} organ slot to the target body part

entity-effect-guidebook-part-remove-slot = { $chance ->
    [1] Removes
   *[other] remove
} the {$slot} organ slot from the target body part

entity-effect-guidebook-regenerate-part = { $chance ->
    [1] Regenerates
   *[other] regenerate
} the {$slot} organ

entity-effect-guidebook-relay-random-part = for a random body part, {$effect}
