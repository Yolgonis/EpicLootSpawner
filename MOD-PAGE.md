## Version 1.4.1 Features

- **Redesigned Single-Page Interface:** Rarity, quality, quantity, sockets, name, effects, and stones are edited on one page with − / + buttons. A top bar switches between new items, your inventory, and presets; an always-visible bottom bar holds delivery and the main action.
- **Pickers With Full Descriptions:** Add effects, socket stones, and choose Legendary/set identities from an overlay where every card shows the in-game description, notes, and normal value range. Pick several in a row before closing.
- **Optional Unlimited Mode:** Effect values outside the normal roll range (including negative), up to 30 effects and 12 sockets. Item-type compatibility, duplicate and conflicting effects, and special-power rules are still enforced. Out-of-range values are highlighted.
- **Unlimited Values Work In-Game:** While Unlimited Mode is on, EpicLoot's internal effect caps are lifted too, including the resistance cap, Bulk Up, Mercenary soft cap, Lucky Loot limits, parry window, slow floor, mead cooldown, night/Off-Set damage reduction, blood cost reduction, Shocking Charge and adrenaline efficiency. Game-breaking limits stay: damage taken never turns into healing, and costs and durations never go below zero.

- **Custom Item Spawning:** Choose the item, quality level, rarity, quantity, and custom name; deliver it to your inventory or drop it on the ground.
- **Loot-Style Item Origin:** Newly spawned items have no crafter ID or name, so the native **Crafted by** line is absent, including special Legendary/set items and bulk spawning.
- **Custom Quality Levels:** Enter a positive whole-number quality above the item's normal maximum. The normal quality range remains visible; effect and socket limits still apply.
- **Effect Editing:** Add effects compatible with the selected item, with localized names and descriptions, value ranges, sliders, − / + steps, and minimum/maximum buttons.
- **Shardstone Management:** Configure sockets and stones, check compatibility, and preview their effects.
- **Special Legendary and Set Items:** Select an eligible special identity for your base item, including native Mythic definitions. View localized descriptions, set membership, and set bonus thresholds. Starting powers use their own configured ranges; ordinary effect compatibility and socket limits remain enforced.
- **Inventory Editor:** Select an existing item and modify it using **Apply to Original Item**. Known special Legendary/set items retain their identity and set membership.
- **Enchantment Removal:** Preview and confirm removal of an inventory item's enchantment. Socketed shardstones return to your inventory; quality and durability percentage are preserved. The operation requires room for every returned stone.
- **Single-Stone Extraction:** Choose one socketed shardstone and return it to your inventory while keeping the enchantment, remaining stones, and socket capacity. Remaining stone effects are recalculated by EpicLoot.
- **Inventory Operation Undo:** Preview and undo the last inventory edit, enchantment removal, or single-stone extraction in the current game session. Returned stones are taken back from inventory; missing stones or external item changes block undo. Current durability percentage and other inventory items are preserved.
- **Detailed Confirmation Panels:** View localized socket effects, before/after changes, final item details, and grouped stone returns in separate tabs. Undo lists missing stone types and quantities; previews follow current durability and game language.
- **Live Preview:** View the in-game item tooltip and compare your design with the original item.
- **Item Library:** Sort by ascending tier and alphabetically within each tier; filter by category, search, and favorites.
- **Design History:** Undo and redo up to 60 design states.
- **Presets:** Save, update, rename, duplicate, delete, and tag item designs.
- **Sharing:** Transfer individual designs through the clipboard and preset collections through JSON files.
- **Persistence and Recovery:** Save your latest design, favorites, and interface preferences; restore data from backups.
- **Interface:** Turkish menus, item and effect text following the game language, a dark theme, adjustable scale, a draggable and resizable window with saved dimensions, in-field search hints, and support for long names.
- **Diagnostics:** Display errors beside the relevant fields and copy a detailed diagnostic report to the clipboard.
