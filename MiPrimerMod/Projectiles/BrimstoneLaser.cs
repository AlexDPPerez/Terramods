using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace MiPrimerMod.Projectiles
{
	// El rayo de azufre: una barra ROJA que nace de la mano del jugador,
	// lo sigue mientras existe y se abre/cierra con una animación.
	public class BrimstoneLaser : ModProjectile
	{
		// Longitud del rayo en píxeles.
		public const float Largo = 1400f;

		// Grosor de la línea de daño.
		public const float Grosor = 20f;

		// Ticks que dura el rayo en pantalla. 30 ticks = 0,5 s.
		public const int Duracion = 30;

		// Ticks que tarda en abrirse al nacer y en cerrarse al morir.
		public const float TicksEntrada = 4f;
		public const float TicksSalida = 7f;

		// Distancia desde el centro del jugador donde nace el rayo (la mano).
		public const float DistanciaMano = 20f;

		public override void SetDefaults()
		{
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.penetrate = -1;      // atraviesa a todos los enemigos
			Projectile.tileCollide = false; // no le importan los bloques
			Projectile.ignoreWater = true;
			Projectile.timeLeft = Duracion;

			// Que cada enemigo reciba el daño UNA sola vez (si no, el rayo
			// golpearía al mismo bicho en cada tick que está vivo).
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
		}

		// 0 al nacer, 1 a mitad de vida, vuelve a bajar al final.
		// Es lo que usan tanto el dibujo como los efectos.
		private float Animacion()
		{
			float entrada = MathHelper.Clamp((Duracion - Projectile.timeLeft) / TicksEntrada, 0f, 1f);
			float salida = MathHelper.Clamp(Projectile.timeLeft / TicksSalida, 0f, 1f);
			return entrada * salida;
		}

		public override void AI()
		{
			// El ángulo viene del arma, en ai[0]. Se queda fijo.
			Projectile.rotation = Projectile.ai[0];
			Projectile.velocity = Vector2.Zero;

			Vector2 direccion = Projectile.rotation.ToRotationVector2();

			// --- El rayo sale de la mano y SIGUE al jugador mientras dura ---
			// Así, si caminas mientras disparas, el rayo se mueve contigo
			// en vez de quedarse clavado donde lo lanzaste.
			Player dueno = Main.player[Projectile.owner];
			if (dueno.active && !dueno.dead)
				Projectile.Center = dueno.Center + direccion * DistanciaMano;

			// Explosión inicial (solo el primer tick).
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				ExplosionInicial(direccion);
			}

			// Chispas rojas saltando a lo largo de todo el rayo.
			for (int i = 0; i < 3; i++)
			{
				float distancia = (float)Main.rand.NextDouble() * Largo;
				Vector2 punto = Projectile.Center + direccion * distancia;

				Dust polvo = Dust.NewDustPerfect(punto, DustID.Blood, Vector2.Zero, 100, default, 1.5f);
				polvo.noGravity = true;
				polvo.color = new Color(255, 25, 15);
				polvo.velocity = direccion.RotatedByRandom(0.5f) * (1f + (float)Main.rand.NextDouble() * 4f);
			}

			// --- Iluminación: luces repartidas por TODO el recorrido del rayo ---
			// Si solo iluminas un punto, el rayo se ve apagado a lo lejos.
			Lighting.AddLight(Projectile.Center, 1.9f, 0.40f, 0.28f);

			const int puntosDeLuz = 8;
			for (int i = 1; i <= puntosDeLuz; i++)
			{
				Vector2 punto = Projectile.Center + direccion * (Largo * i / puntosDeLuz);
				// Se va apagando con la distancia para que no ilumine el mapa entero.
				float intensidad = 1.1f - 0.09f * i;
				Lighting.AddLight(punto, intensidad, 0.18f, 0.12f);
			}
		}

		private void ExplosionInicial(Vector2 direccion)
		{
			for (int i = 0; i < 26; i++)
			{
				Vector2 velocidad = direccion.RotatedByRandom(1.1f) * (1f + (float)Main.rand.NextDouble() * 7f);

				Dust polvo = Dust.NewDustPerfect(Projectile.Center, DustID.Blood, velocidad, 100, default, 1.8f);
				polvo.noGravity = true;
				polvo.color = new Color(255, 60, 45);
			}
		}

		// Colisión personalizada: Terraria usa cajas rectangulares (AABB) para todo,
		// así que un rayo en diagonal necesita comprobar la LÍNEA contra cada enemigo.
		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			Vector2 inicio = Projectile.Center;
			Vector2 fin = inicio + Projectile.rotation.ToRotationVector2() * Largo;
			float puntoDeChoque = 0f;

			return Collision.CheckAABBvLineCollision(
				targetHitbox.TopLeft(), targetHitbox.Size(), inicio, fin, Grosor, ref puntoDeChoque);
		}

		// Dibujo propio con animación de apertura y cierre.
		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D textura = TextureAssets.Projectile[Type].Value;

			float animacion = Animacion();

			// Al nacer empieza fino y algo translúcido; se abre y se cierra al final.
			float ancho = 0.25f + 0.75f * animacion;
			float alfa = 0.35f + 0.65f * animacion;

			// Si el rayo se dibuja "hacia la derecha" en la textura, el pivote
			// es el borde izquierdo y la mitad de la altura.
			Vector2 pivote = new Vector2(0f, textura.Height / 2f);
			float escalaX = Largo / textura.Width;

			Main.EntitySpriteDraw(
				textura,
				Projectile.Center - Main.screenPosition,
				null,
				Color.White * alfa,
				Projectile.rotation,
				pivote,
				new Vector2(escalaX, ancho),
				SpriteEffects.None,
				0f);

			return false; // ya lo dibujamos nosotros, que no lo dibuje el juego
		}
	}
}
