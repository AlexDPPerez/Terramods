using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MiPrimerMod.Projectiles
{
	// Heredar de ModProjectile crea una entidad que vuela por el mundo
	// (como una flecha, una bala o una chispa).
	public class ChispaProyectil : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 12;
			Projectile.height = 12;
			Projectile.friendly = true;                      // daña a enemigos, no al jugador
			Projectile.DamageType = DamageClass.Melee;       // hereda el daño del arma
			Projectile.penetrate = 2;                        // atraviesa 2 enemigos antes de morir
			Projectile.timeLeft = 60;                        // vive 60 ticks = 1 segundo
			Projectile.light = 0.6f;                         // ilumina un poco
			Projectile.ignoreWater = true;                   // no la frena el agua
			Projectile.tileCollide = true;                   // choca contra bloques
			Projectile.extraUpdates = 1;                     // se mueve el doble de fino (menos "saltona")
		}

		// AI se ejecuta cada tick: aquí va el comportamiento del proyectil.
		public override void AI()
		{
			// Que el dibujo apunte hacia donde vuela.
			Projectile.rotation = Projectile.velocity.ToRotation();

			// Rastro de partículas moradas. NextBool(3) = solo 1 de cada 3 ticks,
			// para que no haga lag ni se vea saturado.
			if (Main.rand.NextBool(3))
			{
				Dust dust = Dust.NewDustDirect(
					Projectile.position, Projectile.width, Projectile.height,
					DustID.GemAmethyst);

				dust.noGravity = true;   // flota
				dust.velocity *= 0.2f;   // casi quieta
			}
		}

		// Se llama cuando el proyectil golpea a un enemigo.
		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			// Quema al enemigo durante 2 segundos (120 ticks).
			target.AddBuff(BuffID.OnFire, 120);
		}
	}
}
