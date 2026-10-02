using Assets.HeroEditor.Common.Scripts.Common;
using Assets.HeroEditor.Common.Scripts.CharacterScripts.Firearms;
using HeroEditor.Common;
using HeroEditor.Common.Enums;
using UnityEngine;

namespace Assets.HeroEditor.Common.Scripts.CharacterScripts
{
    /// <summary>
    /// You can extend 'CharacterBase' class here. Alternatively, you can just use derived class 'Character' for adding new features.
    /// </summary>
    public static class CharacterExtensions
    {
        public static Color RandomColor => new(Random.Range(0, 1f), Random.Range(0, 1f), Random.Range(0, 1f), 1f);

        public static void Randomize(this CharacterBase character)
        {
            character.ResetEquipment();

            character.SetBody(character.SpriteCollection.Hair.Random(), BodyPart.Hair, RandomColor);
            character.SetBody(character.SpriteCollection.Eyebrows.Random(), BodyPart.Eyebrows, Color.white);
            character.SetBody(character.SpriteCollection.Eyes.Random(), BodyPart.Eyes, RandomColor);
            character.SetBody(character.SpriteCollection.Mouth.Random(), BodyPart.Mouth, Color.white);

            character.Equip(character.SpriteCollection.Helmet.Random(), EquipmentPart.Helmet, Color.white);
            character.Equip(character.SpriteCollection.Armor.Random(), EquipmentPart.Armor, Color.white);
            character.Equip(character.SpriteCollection.MeleeWeapon1H.Random(), EquipmentPart.MeleeWeapon1H, Color.white);
            character.Equip(character.SpriteCollection.Shield.Random(), EquipmentPart.Shield, Color.white);
        }

        public static Firearm GetFirearm(this CharacterBase character)
        {
            return ((Character) character).Firearm;
        }
    }
}