using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace MiPrimerMod.Projectiles
{
	// La piedra que el jugador sostiene mientras carga.
	// Absorbe partículas del entorno y, al soltar, dispara el rayo.
	public class BrimstoneCarga : ModProjectile
	{
		// Carga mínima (en ticks) para que el disparo salga. 45 ticks = 0,75 s.
		public const int CargaMinima = 45;

		// Ticks hasta la carga máxima. 120 ticks = 2 s.
		public const int CargaMaxima = 120;

		// Radio del anillo del que se absorben las partículas.
		public const float RadioAbsorcion = 70f;

		// Fuerza del retroceso al disparar (px por tick). Súbelo para más empuje.
		public const float Empuje = 3.5f;

		// Distancia desde el centro del jugador hasta la mano donde flota la piedra.
		public const float DistanciaMano = 24f;

		// Ticks que tarda la piedra en hacerse visible al empezar a cargar.
		// 15 ticks = 0,25 s. Súbelo para un fundido más lento.
		public const float TicksAparicion = 15f;

		public override void SetDefaults()
		{
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.friendly = false;   // la piedra en sí no golpea nada
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 2;       // se renueva cada tick mientras cargas
		}

		public override void AI()
		{
			Player player = Main.player[Projectile.owner];

			// --- 1. Mantener la piedra viva mientras el jugador siga pulsando ---
			// Antes usábamos player.channel, pero eso exige Item.channel = true,
			// que congela la animación del personaje. Ahora el arma golpea con
			// auto-repetición y comprobamos directamente el botón de usar.
			bool sosteniendo = player.controlUseItem;

			if (sosteniendo && !player.dead)
				Projectile.timeLeft = 2;

			// Sonido de arranque de la carga (una sola vez).
			if (Projectile.localAI[1] == 0f)
			{
				Projectile.localAI[1] = 1f;
				SoundEngine.PlaySound(SoundID.Item15, Projectile.Center);
			}

			// --- 2. Pegarla a la mano del jugador, apuntando al ratón ---
			Vector2 direccion = (Main.MouseWorld - player.MountedCenter)
				.SafeNormalize(Vector2.UnitX * player.direction);

			// OJO: player.itemLocation NO se actualiza mientras mantienes pulsado
			// (se queda con el valor del primer tick), así que la piedra se quedaría
			// clavada donde empezaste a cargar. Calculamos la mano desde el jugador.
			Projectile.Center = player.MountedCenter + direccion * DistanciaMano;
			Projectile.velocity = direccion; // guardamos la dirección aquí para el disparo

			// --- 3. Contar la carga (se queda en el máximo) ---
			if (Projectile.ai[0] < CargaMaxima)
				Projectile.ai[0]++;

			float progreso = MathHelper.Clamp(Projectile.ai[0] / CargaMaxima, 0f, 1f);

			// Gira despacio sobre sí misma. Se nota porque la textura tiene un remolino.
			Projectile.rotation += 0.015f + 0.02f * progreso;

			// Palpita: más rápido y con más fuerza a medida que carga.
			float frecuencia = 0.10f + 0.14f * progreso;
			float amplitud = 0.035f + 0.05f * progreso;
			float pulso = 1f + amplitud * (float)System.Math.Sin(Main.GameUpdateCount * frecuencia);

			Projectile.scale = (0.6f + 0.5f * progreso) * pulso;

			// --- 4. Absorción: partículas del anillo que vuelan hacia el centro ---
			AbsorberParticulas(progreso);

			// Luz central, que también late con la piedra.
			// Intensidad alta: esto es energía, no una vela.
			float latido = 0.85f + 0.15f * (float)System.Math.Sin(Main.GameUpdateCount * 0.2f);
			Lighting.AddLight(
				Projectile.Center,
				(0.30f + 1.50f * progreso) * latido,
				(0.08f + 0.22f * progreso) * latido,
				(0.06f + 0.18f * progreso) * latido);

			// Dos luces pequeñas girando alrededor: hacen que la piedra "brille en movimiento".
			if (progreso > 0.25f)
			{
				for (int i = 0; i < 2; i++)
				{
					float anguloLuz = Projectile.rotation * 2f + i * MathHelper.Pi;
					Vector2 puntoLuz = Projectile.Center + new Vector2(
						(float)System.Math.Cos(anguloLuz) * 20f,
						(float)System.Math.Sin(anguloLuz) * 20f);

					Lighting.AddLight(puntoLuz, 0.60f * progreso, 0.10f * progreso, 0.08f * progreso);
				}
			}

			// --- 5. Aviso cuando la carga está completa ---
			if (progreso >= 1f)
			{
				// Solo la primera vez: estallido + sonido de "listo".
				if (Projectile.localAI[0] == 0f)
				{
					Projectile.localAI[0] = 1f;
					EstallidoDeCarga();
					SoundEngine.PlaySound(SoundID.MaxMana, Projectile.Center);
				}

				// Mientras siga llena, chispas orbitando.
				ParticulasDeListo();
			}

			// --- 6. ¿Soltaste el botón? Pues dispara (o se apaga si no cargaste) ---
			if (!sosteniendo || player.dead)
			{
				Disparar(player, progreso);
				Projectile.Kill();
			}
		}

		// Crea partículas en un anillo alrededor de la piedra y las lanza hacia dentro.
		private void AbsorberParticulas(float progreso)
		{
			// Cuanta más carga, más partículas por tick (de 1 a 3).
			int cantidad = 1 + (int)(progreso * 2f);

			for (int i = 0; i < cantidad; i++)
			{
				// Tres brazos que giran: las partículas caen en espiral, no al azar.
				float giro = Main.GameUpdateCount * 0.055f;
				float angulo = giro + i * (MathHelper.TwoPi / 3f)
					+ (float)(Main.rand.NextDouble() * 0.5 - 0.25);

				float cos = (float)System.Math.Cos(angulo);
				float sen = (float)System.Math.Sin(angulo);

				Vector2 offset = new Vector2(cos * RadioAbsorcion, sen * RadioAbsorcion);
				Vector2 posicion = Projectile.Center + offset;

				// 1 de cada 4 es una chispa de rubí (más brillante), el resto sangre.
				int tipo = Main.rand.NextBool(4) ? DustID.GemRuby : DustID.Blood;

				Dust polvo = Dust.NewDustPerfect(
					posicion, tipo, Vector2.Zero, 100, default,
					0.9f + (float)Main.rand.NextDouble() * 1.1f);

				polvo.noGravity = true;
				polvo.color = new Color(255, 40, 30);

				// El polvo pierde un 5% de velocidad por tick, así que la velocidad
				// necesaria para recorrer una distancia es distancia / ~17.
				Vector2 haciaCentro = (Projectile.Center - posicion) / 17f;

				// Un poco de giro para que se vea como un remolino, no como líneas rectas.
				Vector2 tangente = new Vector2(-offset.Y, offset.X) * 0.02f;

				polvo.velocity = haciaCentro + tangente;
			}
		}

		// Chispas girando alrededor de la piedra mientras está cargada al máximo.
		private void ParticulasDeListo()
		{
			for (int i = 0; i < 2; i++)
			{
				float angulo = Main.GameUpdateCount * 0.09f + i * MathHelper.Pi;
				Vector2 posicion = Projectile.Center + new Vector2(
					(float)System.Math.Cos(angulo) * 24f,
					(float)System.Math.Sin(angulo) * 24f);

				Dust polvo = Dust.NewDustPerfect(posicion, DustID.GemRuby, Vector2.Zero, 100, default, 1.1f);
				polvo.noGravity = true;
				polvo.color = new Color(255, 60, 50);
			}
		}

		// Anillo giratorio alrededor de la piedra: el indicador de "ya puedes disparar".
		// Aquí dibujamos TODO a mano para que la piedra no se oscurezca de noche.
		public override bool PreDraw(ref Color lightColor)
		{
			if (Projectile.ai[0] >= CargaMaxima)
			{
				Texture2D anillo = ModContent.Request<Texture2D>("MiPrimerMod/Projectiles/BrimstoneListo").Value;

				float pulso = 0.9f + 0.1f * (float)System.Math.Sin(Main.GameUpdateCount * 0.35f);

				Main.EntitySpriteDraw(
					anillo,
					Projectile.Center - Main.screenPosition,
					null,
					Color.White,
					Main.GameUpdateCount * 0.06f,   // gira
					anillo.Size() / 2f,             // pivote en el centro
					pulso,
					SpriteEffects.None,
					0f);
			}

			// La piedra: se dibuja con Color.White, ignorando lightColor.
			// Con el dibujo por defecto Terraria la multiplicaría por la luz del
			// ambiente y de noche se vería casi negra.
			Texture2D piedra = TextureAssets.Projectile[Type].Value;

			// Animación de opacidad: nace invisible (0) y va subiendo hasta 1.
			float alfa = MathHelper.Clamp(Projectile.ai[0] / TicksAparicion, 0f, 1f);

			Main.EntitySpriteDraw(
				piedra,
				Projectile.Center - Main.screenPosition,
				null,
				Color.White * alfa,
				Projectile.rotation,
				piedra.Size() / 2f,
				Projectile.scale,
				SpriteEffects.None,
				0f);

			return false; // ya lo dibujamos todo nosotros
		}

		// Onda de partículas hacia fuera al llegar a la carga máxima.
		private void EstallidoDeCarga()
		{
			for (int i = 0; i < 30; i++)
			{
				float angulo = (float)(Main.rand.NextDouble() * MathHelper.TwoPi);
				Vector2 radial = new Vector2((float)System.Math.Cos(angulo), (float)System.Math.Sin(angulo));

				Dust polvo = Dust.NewDustPerfect(
					Projectile.Center + radial * 10f,
					DustID.Blood,
					radial * (1.5f + (float)Main.rand.NextDouble() * 4.5f),
					100, default, 1.5f);

				polvo.noGravity = true;
				polvo.color = new Color(255, 55, 45);
			}
		}

		private void Disparar(Player player, float progreso)
		{
			// Si no llegó a la carga mínima, no sale nada.
			if (Projectile.ai[0] < CargaMinima)
				return;

			// Del 55% al 100% del daño según lo cargado.
			// Projectile.damage ya trae el daño del arma en el momento del disparo.
			int dano = (int)(Projectile.damage * (0.55f + 0.45f * progreso));

			Vector2 direccion = Projectile.velocity.SafeNormalize(Vector2.UnitX * player.direction);

			// El rayo se crea aquí y "nace" en la mano.
			Projectile.NewProjectile(
				Projectile.GetSource_FromThis(),
				Projectile.Center,
				Vector2.Zero,
				ModContent.ProjectileType<BrimstoneLaser>(),
				dano,
				Projectile.knockBack,
				Projectile.owner,
				direccion.ToRotation()); // ai[0] = ángulo del rayo

			// --- RETROCESO: empujón al jugador en sentido contrario al disparo ---
			player.velocity -= direccion * Empuje;

			// Sonido del disparo.
			SoundEngine.PlaySound(SoundID.Item12, Projectile.Center);

			// Explosión de sangre en la boca del arma.
			for (int i = 0; i < 45; i++)
			{
				Vector2 velocidad = direccion.RotatedByRandom(0.55f) * (2f + (float)Main.rand.NextDouble() * 9f);
				Dust polvo = Dust.NewDustPerfect(Projectile.Center, DustID.Blood, velocidad, 100, default, 1.6f);
				polvo.noGravity = true;
				polvo.color = new Color(255, 40, 30);
			}
		}
	}
}
