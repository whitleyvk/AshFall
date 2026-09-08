# SPDX-License-Identifier: AGPL-3.0-or-later

surgery-verb-text = Начать операцию
surgery-verb-message = Провести операцию.
surgery-ui-window-title = Хирургия
surgery-ui-window-parts = < Части тела
surgery-ui-window-surgeries = < Операции
surgery-ui-window-steps = < Шаги
surgery-ui-window-steps-error-skills = Вы не умеете проводить операции.
surgery-ui-window-steps-error-table = Пациента необходимо уложить на операционный стол.
surgery-ui-window-steps-error-armor = Сначала снимите с пациента броню!
surgery-ui-window-steps-error-tools = Нет подходящих инструментов.
surgery-error-laying = Пациент должен лежать!
surgery-error-self-surgery = Вы не можете оперировать себя!
surgery-part-damage-evaded = {$user} едва не получает травму!

surgery-tool-turn-on = Сначала включите инструмент!
surgery-tool-reload = Сначала перезарядите инструмент!
surgery-tool-match-light = Сначала зажгите спичку!
surgery-tool-match-replace = Возьмите новую спичку!

surgery-tool-examinable-verb-text = Хирургический инструмент
surgery-tool-examinable-verb-message = Посмотреть, для каких этапов операции подходит этот инструмент.
surgery-tool-header = Подходит для следующих этапов операций:
surgery-tool-unlimited = - {$tool}, скорость [color={$color}]×{$speed}[/color]
surgery-tool-used = - {$tool}, скорость [color={$color}]×{$speed}[/color], [color=red]расходуется после применения[/color]

surgery-popup-step-SurgeryStepOpenIncisionScalpel = {$user} делает разрез с помощью {$tool} ({$target}, область: {$part}).
surgery-popup-step-SurgeryStepRetractSkin = {$user} разводит края разреза с помощью {$tool} ({$target}, область: {$part}).
surgery-popup-step-SurgeryStepClampBleeders = {$user} зажимает кровоточащие сосуды с помощью {$tool} ({$target}, область: {$part}).
surgery-popup-step-SurgeryStepCloseBloodOutputs = {$user} прижигает кровоточащие сосуды с помощью {$tool} ({$target}, область: {$part}).
surgery-popup-step-SurgeryStepSawBones = {$user} распиливает кость с помощью {$tool} ({$target}, область: {$part}).
surgery-popup-step-SurgeryStepPriseOpenBones = {$user} разводит распиленные кости с помощью {$tool} ({$target}, область: {$part}).
surgery-popup-step-SurgeryStepMendBones = {$user} сопоставляет костные отломки с помощью {$tool} ({$target}, область: {$part}).
surgery-popup-step-SurgeryStepCloseBones = {$user} сводит кости с помощью {$tool} ({$target}, область: {$part}).
surgery-popup-step-SurgeryStepSealBones = {$user} фиксирует кости с помощью {$tool} ({$target}, область: {$part}).
surgery-popup-step-SurgeryStepSealRibcage = {$user} фиксирует рёбра с помощью {$tool} ({$target}, область: {$part}).
surgery-popup-step-SurgeryStepSealSkull = {$user} фиксирует кости черепа с помощью {$tool} ({$target}, область: {$part}).
surgery-popup-step-SurgeryStepCloseIncision = {$user} накладывает швы с помощью {$tool} ({$target}, область: {$part}).
surgery-popup-step-SurgeryStepSawFeature = {$user} перепиливает кость с помощью {$tool} ({$target}, область: {$part}).
surgery-popup-step-SurgeryStepClampInternalBleeders = {$user} зажимает внутренние кровоточащие сосуды с помощью {$tool} ({$target}, область: {$part}).
surgery-popup-step-SurgeryStepRemoveFeature = {$user} проводит ампутацию с помощью {$tool} ({$target}, область: {$part})!
surgery-popup-step-SurgeryStepInsertFeature = {$user} прикрепляет часть тела с помощью {$tool} ({$target}, область: {$part}).
surgery-popup-step-SurgeryStepSealWounds = {$user} обрабатывает и ушивает раны с помощью {$tool} ({$target}, область: {$part}).
surgery-popup-step-SurgeryStepCarefulIncisionScalpel = {$user} делает точный разрез с помощью {$tool} ({$target}, область: {$part}).
surgery-popup-step-SurgeryStepRepairBruteTissue = {$user} восстанавливает повреждённые ткани с помощью {$tool} ({$target}, область: {$part}).
surgery-popup-step-SurgeryStepRepairBurnTissue = {$user} восстанавливает обожжённые ткани с помощью {$tool} ({$target}, область: {$part}).
surgery-popup-step-SurgeryStepSealTendWound = {$user} закрывает и ушивает рану с помощью {$tool} ({$target}, область: {$part}).
surgery-popup-step-SurgeryStepSealDismembermentWound = {$user} формирует и ушивает культю с помощью {$tool} ({$target}, область: {$part}).
surgery-popup-step-SurgeryStepRemoveOrgan = {$user} извлекает орган с помощью {$tool} ({$target}, область: {$part})!
surgery-popup-step-SurgeryStepInsertOrgan = {$user} устанавливает орган с помощью {$tool} ({$target}, область: {$part}).
surgery-popup-step-SurgeryStepSealOrganWound = {$user} закрывает операционную рану с помощью {$tool} ({$target}, область: {$part}).
surgery-popup-step-SurgeryStepOpenOrganSlot = {$user} подготавливает полость для органа с помощью {$tool} ({$target}, область: {$part}).
surgery-popup-step-SurgeryStepInsertItem = {$user} помещает предмет в операционную полость ({$target}, область: {$part}).
surgery-popup-step-SurgeryStepRemoveItem = {$user} извлекает предмет из операционной полости ({$target}, область: {$part}).

surgery-popup-step-SurgeryStepRemoveOrgan-failed = Не удалось извлечь орган...
surgery-popup-step-SurgeryStepRemovePart-failed = Не удалось ампутировать часть тела...
surgery-ui-window-requires = Требуется: {$requirement}

# Процедуры
ent-SurgeryBase = хирургическая операция
ent-BasePartSurgery = хирургическая операция
ent-SurgeryAttachBase = прикрепление части тела
ent-SurgeryOpenIncision = хирургический разрез
ent-SurgeryStopBloodOutput = остановка кровотечения
ent-SurgeryFixDismemberment = обработка культи
ent-SurgeryCloseIncision = ушивание разреза
ent-SurgeryCloseIncisionHead = ушивание разреза на голове
ent-SurgeryCloseIncisionTorso = ушивание разреза на туловище
ent-SurgeryOpenRibcage = вскрытие грудной клетки
ent-SurgeryRemovePart = ампутация
ent-SurgeryMendBones = вправление перелома
ent-SurgeryHealOrgans = восстановление органов
ent-SurgeryAttachHead = прикрепление головы
ent-SurgeryAttachLeftArm = прикрепление левой руки
ent-SurgeryAttachRightArm = прикрепление правой руки
ent-SurgeryAttachLeftLeg = прикрепление левой ноги
ent-SurgeryAttachRightLeg = прикрепление правой ноги
ent-SurgeryAttachLeftHand = прикрепление левой кисти
ent-SurgeryAttachRightHand = прикрепление правой кисти
ent-SurgeryAttachLeftFoot = прикрепление левой стопы
ent-SurgeryAttachRightFoot = прикрепление правой стопы
ent-SurgeryAttachTail = прикрепление хвоста
ent-SurgeryTendWoundsBrute = обработка механических травм
ent-SurgeryTendWoundsBurn = обработка ожогов
ent-SurgeryInsertItem = имплантация предмета
ent-SurgeryInsertBrain = трансплантация мозга
ent-SurgeryInsertEyes = трансплантация глаз
ent-SurgeryInsertEars = трансплантация ушей
ent-SurgeryInsertHeart = трансплантация сердца
ent-SurgeryInsertLungs = трансплантация лёгких
ent-SurgeryInsertLiver = трансплантация печени
ent-SurgeryInsertStomach = трансплантация желудка
ent-SurgeryRemoveBrain = извлечение мозга
ent-SurgeryRemoveEyes = извлечение глаз
ent-SurgeryRemoveEars = извлечение ушей
ent-SurgeryRemoveHeart = извлечение сердца
ent-SurgeryRemoveLungs = извлечение лёгких
ent-SurgeryRemoveLiver = извлечение печени
ent-SurgeryRemoveStomach = извлечение желудка

# Шаги операций
ent-SurgeryStepBase = этап операции
ent-SurgeryStepOpenIncisionScalpel = сделать разрез
ent-SurgeryStepClampBleeders = зажать кровоточащие сосуды
ent-SurgeryStepCloseBloodOutputs = прижечь кровоточащие сосуды
ent-SurgeryStepRetractSkin = развести края разреза
ent-SurgeryStepRemoveSeveredSkin = удалить повреждённую кожу
ent-SurgeryStepSawBones = распилить кость
ent-SurgeryStepRemoveLeftoverBones = удалить костные осколки
ent-SurgeryStepMendBones = сопоставить костные отломки
ent-SurgeryStepHealOrgans = восстановить ткани органов
ent-SurgeryStepPriseOpenBones = развести распиленные кости
ent-SurgeryStepCloseBones = свести кости
ent-SurgeryStepSealBones = зафиксировать кости
ent-SurgeryStepSealSkull = зафиксировать кости черепа
ent-SurgeryStepSealRibcage = зафиксировать рёбра
ent-SurgeryStepCloseIncision = наложить швы
ent-SurgeryStepInsertFeature = прикрепить часть тела
ent-SurgeryStepSealWounds = обработать и ушить раны
ent-SurgeryStepSawFeature = перепилить кость
ent-SurgeryStepClampInternalBleeders = зажать внутренние кровоточащие сосуды
ent-SurgeryStepRemoveFeature = ампутировать часть тела
ent-SurgeryStepCarefulIncisionScalpel = сделать точный разрез
ent-SurgeryStepRepairBruteTissue = восстановить повреждённые ткани
ent-SurgeryStepRepairBurnTissue = восстановить обожжённые ткани
ent-SurgeryStepSealTendWound = закрыть и ушить рану
ent-SurgeryStepSealDismembermentWound = сформировать и ушить культю
ent-SurgeryStepInsertItem = поместить предмет в операционную полость
ent-SurgeryStepRemoveItem = извлечь предмет из операционной полости
ent-SurgeryStepRemoveOrgan = извлечь орган
ent-SurgeryStepInsertOrgan = установить орган
ent-SurgeryStepOpenOrganSlot = подготовить полость для органа
ent-SurgeryStepSealOrganWound = закрыть операционную рану

# Инструменты
ent-Bonesetter = костоправ
    .desc = Хирургический инструмент для сопоставления и фиксации костных отломков.
ent-BoneGel = костный гель
    .desc = Медицинский полимерный гель для стимуляции остеогенеза и восстановления костной ткани.
ent-MedicalStitches = хирургические нити
    .desc = Тонкая атравматическая игла с саморассасывающейся шовной нитью.
ent-OmnimedTool = хирургический мультитул
    .desc = Универсальный хирургический инструмент, заменяющий стандартный операционный набор.

surgery-ui-window-title-part = Хирургия: {$part}
surgery-ui-window-title-operation = Хирургия: {$part}, {$surgery}
surgery-ui-window-tool-required = Необходимый инструмент: {$tool}.
surgery-popup-self = себя
surgery-popup-step-SurgeryStepRemoveSeveredSkin = {$user} удаляет повреждённую кожу с помощью {$tool} ({$target}, область: {$part}).
surgery-popup-step-SurgeryStepRemoveLeftoverBones = {$user} удаляет костные осколки с помощью {$tool} ({$target}, область: {$part}).
surgery-popup-step-SurgeryStepHealOrgans = {$user} восстанавливает ткани органов с помощью {$tool} ({$target}, область: {$part}).
