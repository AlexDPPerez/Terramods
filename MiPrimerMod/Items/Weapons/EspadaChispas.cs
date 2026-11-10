using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MiPrimerMod.Items.Weapons
{
	// Heredar de ModItem convierte esta clase en un item del juego.
	public class EspadaChispas : ModItem
	{
		// SetDefaults define los números base del item.
		// Se llama una sola vez, al cargar el mod.
		public override void SetDefaults()
		{
			Item.damage = 24;                        // daño base
			Item.DamageType = DamageClass.Melee;     // es cuerpo a cuerpo (afecta a sus bonuses)
			Item.width = 40;                         // tamaño del hitbox del item (no del dibujo)
			Item.height = 40;
			Item.useTime = 18;                       // ticks entre golpes (60 ticks = 1 segundo)
			Item.useAnimation = 18;                  // duración de la animación de golpe
			Item.useStyle = ItemUseStyleID.Swing;    // se usa con un golpe de espada
			Item.knockBack = 5f;                     // empuje al enemigo
			Item.value = Item.sellPrice(silver: 30); // precio de venta
			Item.rare = ItemRarityID.Blue;           // color del nombre (rareza)
			Item.UseSound = SoundID.Item1;           // sonido al atacar
			Item.autoReuse = true;                   // se puede mantener el clic
			Item.useTurn = true;                     // puedes girar mientras atacas

			// Al atacar, dispara el proyectil que definimos en la otra carpeta.
			Item.shoot = ModContent.ProjectileType<Projectiles.ChispaProyectil>();
			Item.shootSpeed = 9f;
		}

		// AddRecipes define cómo se craftea.
		public override void AddRecipes()
		{
			CreateRecipe()
				.AddRecipeGroup(RecipeGroupID.IronBar, 10) // acepta hierro O plomo
				.AddIngredient(ItemID.Amethyst, 3)
				.AddTile(TileID.Anvils)                    // en un yunque
				.Register();

			// RECETA DE PRUEBA (opcional): quita las dos barras de abajo para craftearla
			// con 10 de madera en la mesa de trabajo, sin buscar minerales.
			// CreateRecipe()
			// 	.AddRecipeGroup(RecipeGroupID.Wood, 10)
			// 	.AddTile(TileID.WorkBenches)
			// 	.Register();
		}
	}
}
