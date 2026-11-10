using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MiPrimerMod.Items.Weapons
{
	// Arma inspirada en "Brimstone" de The Binding of Isaac:
	// mantienes pulsado para cargar y al soltar disparas un rayo que lo atraviesa todo.
	//
	// IMPORTANTE: el nombre de la clase (Brimstone) debe coincidir con el archivo
	// Brimstone.cs y con la textura Brimstone.png. Si no, el mod no carga.
	public class Brimstone : ModItem
	{
		public override void SetDefaults()
		{
			Item.damage = 55;
			Item.DamageType = DamageClass.Magic;  // escala con daño mágico

			// Sin coste de maná: con golpes cada 6 ticks, Terraria cobraría el maná
			// en CADA golpe y te quedarías seco en un segundo.
			Item.mana = 0;

			Item.width = 40;
			Item.height = 40;

			// Golpes rápidos y repetidos: así el personaje mantiene la animación
			// de espada mientras cargas, igual que con la Zenith.
			Item.useTime = 6;
			Item.useAnimation = 6;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.autoReuse = true;

			// Ocultamos el dibujo del arma en la mano: solo se ve la piedra de carga.
			Item.noUseGraphic = true;

			// El golpe es solo animación: no hace daño cuerpo a cuerpo.
			Item.noMelee = true;

			Item.knockBack = 8f;
			Item.value = Item.sellPrice(gold: 2);
			Item.rare = ItemRarityID.LightRed;

			// Sin sonido por golpe: si no, sonaría 10 veces por segundo.
			// El sonido de carga lo lanza la propia piedra, una sola vez.

			// Lo que aparece al usar el arma: la piedra de carga (no el rayo).
			Item.shoot = ModContent.ProjectileType<Projectiles.BrimstoneCarga>();
			Item.shootSpeed = 0f;
		}

		// Mientras mantienes pulsado, el juego intentaría crear otra piedra de carga
		// cada 6 ticks. Esto lo evita: solo puede existir una a la vez.
		public override bool CanShoot(Player player)
		{
			int tipoCarga = ModContent.ProjectileType<Projectiles.BrimstoneCarga>();
			return player.ownedProjectileCounts[tipoCarga] == 0;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.HellstoneBar, 8)
				.AddIngredient(ItemID.Obsidian, 5)
				.AddIngredient(ItemID.Diamond, 2)
				.AddTile(TileID.Anvils)
				.Register();

			// RECETA DE PRUEBA: quita las dos barras de abajo para craftearla con
			// 10 de madera en la mesa de trabajo (útil para probar sin bajar al infierno).
			// CreateRecipe()
			// 	.AddRecipeGroup(RecipeGroupID.Wood, 10)
			// 	.AddTile(TileID.WorkBenches)
			// 	.Register();
		}
	}
}
