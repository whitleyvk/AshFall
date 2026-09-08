# SPDX-License-Identifier: AGPL-3.0-or-later

popup-trauma-BoneDamage-Damaged = Вы чувствуете боль в районе {$part}.
popup-trauma-BoneDamage-Cracked = Вы чувствуете резкую боль в районе {$part}!
popup-trauma-BoneDamage-Broken = Вы слышите громкий хруст в районе {$part}!!
self-inspect-trauma-BoneDamage = болит изнутри
self-inspect-trauma-BoneDamage-Large = кажется вывихнутым
inspect-trauma-BoneDamage = выглядит неестественно вывихнутым

popup-trauma-OrganDamage-Damaged = В районе {$part} что-то сильно не так...
popup-trauma-OrganDamage-Destroyed = Вы чувствуете острую нестерпимую боль в районе {$part}!
self-inspect-trauma-OrganDamage = вызывает странные ощущения при каждом вдохе.

speech-needs-tongue = Без языка невозможно говорить!
eating-needs-organ-no-mouth-self = У вас нет рта!
eating-needs-organ-no-mouth-other = У {$target} нет рта!

installable-organ-examine = Этот орган можно [bold]использовать[/bold], чтобы установить его себе.
installable-organ-already-installed = У вас уже есть орган категории «{$organ}»!
installable-organ-installed = Вы устанавливаете себе орган: {$organ}.

equip-part-missing-error = У {$target} отсутствует необходимая часть тела: {$part}.

reagent-effect-guidebook-adjust-bone-damage = Снижает повреждение костей на {$amount}

entity-condition-guidebook-has-organ = у цели { $invert ->
    [true] отсутствует
   *[false] есть
} орган категории {$organ}

entity-condition-guidebook-organ-slot = у цели { $inverted ->
    [true] отсутствует
   *[false] есть
} место для органа {$slot} в части тела {$part}

entity-effect-guidebook-detach-part = { $chance ->
    [1] Отделяет
   *[other] отделить
} часть тела от тела

entity-effect-guidebook-gib = { $chance ->
    [1] Расчленяет
   *[other] расчленить
} цель

entity-effect-guidebook-insert-new-organ = { $chance ->
    [1] Устанавливает
   *[other] установить
} орган {$organ} в выбранную часть тела

entity-effect-guidebook-move-organ = { $chance ->
    [1] Перемещает
   *[other] переместить
} орган {$organ} в {$dest}

entity-effect-guidebook-part-add-slot = { $chance ->
    [1] Добавляет
   *[other] добавить
} место для органа {$slot} в выбранную часть тела

entity-effect-guidebook-part-remove-slot = { $chance ->
    [1] Удаляет
   *[other] удалить
} место для органа {$slot} из выбранной части тела

entity-effect-guidebook-regenerate-part = { $chance ->
    [1] Восстанавливает
   *[other] восстановить
} орган категории {$slot}

entity-effect-guidebook-relay-random-part = для случайной части тела: {$effect}
