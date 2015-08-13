﻿#region Using Statements
using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.Input;
using Shooter;

#endregion

namespace MyGame
{
	/// <summary>
	/// This is the main type for your game.
	/// </summary>
	public class Game1 : Game {
		private const int HEALTH_MAX = 100;
		enum GameStates { Start, Playing, GameOver }
		GameStates GameState;

		enum MenuOptions { Start=300, Restart=300, Quit=340 }
		MenuOptions menuCursor;

		GraphicsDeviceManager graphics;
		SpriteBatch spriteBatch;

		KeyboardState currentKeyboardState;
		GamePadState currentGamePadState;

		Player player;
		float playerMoveSpeed;
		float scale = 1f;

		Texture2D mainBackground;
		Rectangle rectBackground;
		ParallaxingBackground bgLayer1;
		ParallaxingBackground bgLayer2;
		Texture2D gameOverScreen;
		Texture2D startMenu;

		Texture2D enemyTexture;
		List<Enemy> enemies;
		TimeSpan enemySpawnTime;
		TimeSpan previousSpawnTime;
		Random random;

		Texture2D explosionTexture;
		List<Explosion> explosions;

		Texture2D laserTexture;
		List<Laser> lasers;
		TimeSpan laserSpawnTime;
		TimeSpan previousLaserSpawnTime;

		SoundEffect sfx_explode;
		SoundEffect sfx_laser;

		Song menuMusic;
		Song gameMusic;

		Texture2D healthBar;
		Rectangle healthBarRec;
		int healthBarVal;

		SpriteFont font;
		int score;

		public Game1 ()
		{
			graphics = new GraphicsDeviceManager (this);
			Content.RootDirectory = "Content";	            
			graphics.IsFullScreen = false;		
		}

		/// <summary>
		/// Allows the game to perform any initialization it needs to before starting to run.
		/// This is where it can query for any required services and load any non-graphic
		/// related content.  Calling base.Initialize will enumerate through any components
		/// and initialize them as well.
		/// </summary>
		protected override void Initialize ()
		{
			player = new Player();
			playerMoveSpeed = 8.0f;

			bgLayer1 = new ParallaxingBackground ();
			bgLayer2 = new ParallaxingBackground ();

			enemies = new List<Enemy> ();
			previousSpawnTime = TimeSpan.Zero;
			enemySpawnTime = TimeSpan.FromSeconds (1.0f);
			random = new Random ();

			explosions = new List<Explosion> ();
			lasers = new List<Laser> ();
			laserSpawnTime = TimeSpan.FromSeconds (0.5f);

			GameState = GameStates.Start;
			healthBarVal = HEALTH_MAX;
			score = 0;

			menuCursor = MenuOptions.Start;
			base.Initialize ();
		}

		/// <summary>
		/// LoadContent will be called once per game and is the place to load
		/// all of your content.
		/// </summary>
		protected override void LoadContent ()
		{
			// Create a new SpriteBatch, which can be used to draw textures.
			spriteBatch = new SpriteBatch (GraphicsDevice);

			gameOverScreen = Content.Load<Texture2D> ("Graphics\\endMenu");
			startMenu = Content.Load<Texture2D> ("Graphics\\mainMenu");

			Texture2D playerTexture = Content.Load<Texture2D> ("Graphics\\shipAnimation");
			Animation playerAnimation = new Animation ();
			playerAnimation.Initialize (playerTexture, Vector2.Zero, 115, 69, 8, 30, Color.White, scale, true);
			Vector2 playerPosition = new Vector2 (GraphicsDevice.Viewport.TitleSafeArea.X,
				                         GraphicsDevice.Viewport.TitleSafeArea.Y + GraphicsDevice.Viewport.TitleSafeArea.Height / 2);
			player.Initialize (playerAnimation, playerPosition, HEALTH_MAX);

			bgLayer1.Initialize (Content, "Graphics\\bgLayer1", GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, -1);
			bgLayer2.Initialize (Content, "Graphics\\bgLayer2", GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, -2);
			mainBackground = Content.Load<Texture2D> ("Graphics\\mainbackground");
			rectBackground = new Rectangle (0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

			enemyTexture = Content.Load<Texture2D> ("Graphics\\mineAnimation");
			explosionTexture = Content.Load<Texture2D> ("Graphics\\explosion");
			laserTexture = Content.Load<Texture2D> ("Graphics\\laser");

			sfx_explode = Content.Load<SoundEffect> ("Sound\\explosion");
			sfx_laser = Content.Load<SoundEffect> ("Sound\\laserFire");

			menuMusic = Content.Load<Song> ("Sound\\menuMusic");
			gameMusic = Content.Load<Song> ("Sound\\gameMusic");
			MediaPlayer.IsRepeating = true;

			healthBar = Content.Load<Texture2D> ("Graphics\\healthBar");
			healthBarRec = new Rectangle (15, 15, healthBar.Width, healthBar.Height);

			font = Content.Load<SpriteFont> ("gameFont");
		}

		/// <summary>
		/// Allows the game to run logic such as updating the world,
		/// checking for collisions, gathering input, and playing audio.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Update (GameTime gameTime) {
			// For Mobile devices, this logic will close the Game when the Back button is pressed
			// Exit() is obsolete on iOS
			#if !__IOS__
			if (GamePad.GetState (PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
			    Keyboard.GetState ().IsKeyDown (Keys.Escape)) {
				Exit ();
			}
			#endif

			currentKeyboardState = Keyboard.GetState ();
			currentGamePadState = GamePad.GetState (PlayerIndex.One);

			switch(GameState) {
				case GameStates.Start:
					UpdateStartMenu ();
					break;
				case GameStates.GameOver:
					UpdateGameOverScreen();
					break;
				case GameStates.Playing:
					UpdateGamePlay(gameTime);
					break;
			}
			base.Update (gameTime);

		}

		private void UpdateStartMenu() {
			if ( MediaPlayer.State != MediaState.Playing )
				MediaPlayer.Play (menuMusic);

			if (currentKeyboardState.IsKeyDown (Keys.Enter) || currentGamePadState.Buttons.Start == ButtonState.Pressed) {
				if (menuCursor == MenuOptions.Quit)
					Exit ();

				GameState = GameStates.Playing;
				MediaPlayer.Stop ();
			}

			if (currentKeyboardState.IsKeyDown (Keys.Down) || currentGamePadState.DPad.Down == ButtonState.Pressed) {
				menuCursor = MenuOptions.Quit;
			}
			if (currentKeyboardState.IsKeyDown (Keys.Up) || currentGamePadState.DPad.Up == ButtonState.Pressed) {
				menuCursor = MenuOptions.Start;
			}
		}

		private void UpdateGameOverScreen() {
			if (currentKeyboardState.IsKeyDown (Keys.Enter) || currentGamePadState.Buttons.Start == ButtonState.Pressed) {
				if (menuCursor == MenuOptions.Quit)
					Exit ();

				player.Health = healthBarVal = HEALTH_MAX;
				score = 0;
				GameState = GameStates.Playing;
				player.Active = true;
				player.Position.X = GraphicsDevice.Viewport.TitleSafeArea.X;
				player.Position.Y = GraphicsDevice.Viewport.TitleSafeArea.Y + GraphicsDevice.Viewport.TitleSafeArea.Height / 2;
				healthBarRec.Width = healthBar.Width;
				enemies.Clear ();
			}

			if (currentKeyboardState.IsKeyDown (Keys.Down) || currentGamePadState.DPad.Down == ButtonState.Pressed) {
				menuCursor = MenuOptions.Quit;
			}
			if (currentKeyboardState.IsKeyDown (Keys.Up) || currentGamePadState.DPad.Up == ButtonState.Pressed) {
				menuCursor = MenuOptions.Restart;
			}
		}

		private void UpdateGamePlay(GameTime gt) {
			if (!player.Active && explosions.Count <= 0) {
				GameState = GameStates.GameOver;
				MediaPlayer.Stop ();
				return;
			}

			if (MediaPlayer.State != MediaState.Playing)
				MediaPlayer.Play (gameMusic);	
	
			if (player.Health < healthBarVal) {
				healthBarVal--;
				healthBarRec.Width = (int)(healthBar.Width * (healthBarVal / (float)HEALTH_MAX));
			}

			UpdatePlayer (gt);
			bgLayer1.Update (gt);
			bgLayer2.Update (gt);
			UpdateEnemies (gt);
			UpdateExplosions (gt);
			UpdateLasers (gt);
			UpdateCollision ();
		}

		private void UpdatePlayer(GameTime gameTime) {
			if (!player.Active) return;
			player.Position.X += currentGamePadState.ThumbSticks.Left.X * playerMoveSpeed;
			player.Position.Y -= currentGamePadState.ThumbSticks.Left.Y * playerMoveSpeed;

			if (currentKeyboardState.IsKeyDown (Keys.Left) || currentGamePadState.DPad.Left == ButtonState.Pressed) {
				player.Position.X -= playerMoveSpeed;
			}
			if (currentKeyboardState.IsKeyDown (Keys.Right) || currentGamePadState.DPad.Right == ButtonState.Pressed) {
				player.Position.X += playerMoveSpeed;
			}
			if (currentKeyboardState.IsKeyDown (Keys.Up) || currentGamePadState.DPad.Up == ButtonState.Pressed) {
				player.Position.Y -= playerMoveSpeed;
			}
			if (currentKeyboardState.IsKeyDown (Keys.Down) || currentGamePadState.DPad.Down == ButtonState.Pressed) {
				player.Position.Y += playerMoveSpeed;
			}

			if (currentKeyboardState.IsKeyDown (Keys.Space) || currentGamePadState.Buttons.A == ButtonState.Pressed) {
				if (gameTime.TotalGameTime - previousLaserSpawnTime > laserSpawnTime) {
					previousLaserSpawnTime = gameTime.TotalGameTime;
					AddLaser (player.Position, 80, 25);
				}
			}

			player.Position.X = MathHelper.Clamp (player.Position.X, 0, GraphicsDevice.Viewport.Width - player.Width);
			player.Position.Y = MathHelper.Clamp (player.Position.Y, 0, GraphicsDevice.Viewport.Height - player.Height);
			player.Update (gameTime);
		}

		private void UpdateEnemies(GameTime gameTime) {
			if (gameTime.TotalGameTime - previousSpawnTime > enemySpawnTime) {
				previousSpawnTime = gameTime.TotalGameTime;
				AddEnemy ();
			}

			for (int i = enemies.Count - 1; i >= 0; i--) {
				enemies [i].Update (gameTime);
				if (!enemies [i].Active) {
					enemies.RemoveAt (i);
				}
			}
		}

		private void UpdateExplosions(GameTime gameTime) {
			for (int i = explosions.Count - 1; i >= 0; i--) {
				explosions[i].Update(gameTime);
				if (!explosions[i].Active) {
					explosions.RemoveAt(i);
				}
			}
		}

		private void UpdateLasers(GameTime gameTime) {
			for (int i = lasers.Count - 1; i >= 0; i--) {
				lasers[i].Update(gameTime, GraphicsDevice.Viewport.Width);
				if (!lasers[i].Active) {
					lasers.RemoveAt(i);
				}
			}
		}

		private void UpdateCollision() {
			Rectangle rectangle1 = new Rectangle ((int)player.Position.X, (int)player.Position.Y, player.Width, player.Height);
			Rectangle rectangle2;
			Rectangle laserRect;

			for (int i = 0; i < enemies.Count; i++) {
				rectangle2 = new Rectangle ((int)enemies [i].Position.X, (int)enemies [i].Position.Y, enemies [i].Width, enemies [i].Height);
				if (rectangle1.Intersects (rectangle2)) {
					player.Health -= enemies [i].Damage;
					enemies [i].Health = 0;

					// switch to an explosion animation
					AddExplosion (enemies [i].Position, -50, -30, enemies [i].Speed, 30);

					if (player.Health <= 0) {
						AddExplosion (player.Position, 0, -30, 0f, 200);
						player.Active = false;
						player.Position = Vector2.Zero;
					}
				}

				// See if any lasers collide
				for (int j = 0; j < lasers.Count; j++) {
					laserRect = new Rectangle ((int)lasers [j].Position.X, (int)lasers [j].Position.Y, lasers [j].Width, lasers [j].Height);
					if (laserRect.Intersects (rectangle2)) {
						enemies [i].Health = 0;
						AddExplosion (enemies [i].Position, -50, -30, enemies [i].Speed, 30);
						lasers [j].Active = false;
						score += enemies [i].Value;
					}
				}
			}
		}

		private void AddEnemy() {
			Animation enemyAnimation = new Animation ();
			enemyAnimation.Initialize (enemyTexture, Vector2.Zero, 47, 61, 8, 30, Color.White, scale, true);
			Vector2 position = new Vector2 (GraphicsDevice.Viewport.Width + enemyTexture.Width / 2,
				random.Next (50, GraphicsDevice.Viewport.Height - 50));

			Enemy enemy = new Enemy ();
			enemy.Initialize (enemyAnimation, position);
			enemies.Add (enemy);
		}


		private void AddLaser(Vector2 position, int xOffset, int yOffset) {
			position.X += xOffset;
			position.Y += yOffset;
			Laser laser = new Laser ();
			laser.Initialize (laserTexture, position);
			lasers.Add (laser);
			sfx_laser.Play ();
		}

		private void AddExplosion(Vector2 position, int xOffset, int yOffset, float velocity, int animationSpeedMS) {
			position.X += xOffset;
			position.Y += yOffset;

			Animation explosionAnimation = new Animation ();
			explosionAnimation.Initialize (explosionTexture, Vector2.Zero, 133, 134, 12, animationSpeedMS, Color.White, scale, false);
			Explosion ex = new Explosion ();
			ex.Initialize (explosionAnimation, position, velocity);
			explosions.Add (ex);

			sfx_explode.Play ();
		}

		/// <summary>
		/// This is called when the game should draw itself.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Draw (GameTime gameTime)
		{
			//graphics.GraphicsDevice.Clear (Color.CornflowerBlue);
			graphics.GraphicsDevice.Clear (Color.CornflowerBlue);

			spriteBatch.Begin ();

			switch (GameState) {
				case GameStates.Start:
					DrawStartMenu ();
					break;
				case GameStates.GameOver:
					DrawGameOverScreen ();
					break;
				case GameStates.Playing:
					DrawGamePlay ();
					break;
			}

			spriteBatch.End ();
			base.Draw (gameTime);
		}

		private void DrawStartMenu() {
			spriteBatch.Draw (startMenu, rectBackground, Color.White);

			spriteBatch.DrawString (font, ">", new Vector2 (325, (int)menuCursor), Color.White);
			spriteBatch.DrawString (font, "Start", new Vector2 (350, 300), Color.White);
			spriteBatch.DrawString (font, "Quit", new Vector2 (350, 340), Color.White);
		}

		private void DrawGameOverScreen() {
			spriteBatch.Draw (gameOverScreen, rectBackground, Color.White);

			spriteBatch.DrawString (font, "Final Score: " + score, new Vector2 (300, 200), Color.White);

			spriteBatch.DrawString (font, ">", new Vector2 (325, (int)menuCursor), Color.White);
			spriteBatch.DrawString (font, "Restart", new Vector2 (350, 300), Color.White);
			spriteBatch.DrawString (font, "Quit", new Vector2 (350, 340), Color.White);
		}

		private void DrawGamePlay() {
			spriteBatch.Draw (mainBackground, rectBackground, Color.White);
			bgLayer1.Draw (spriteBatch);
			bgLayer2.Draw (spriteBatch);

			for (int i = 0; i < enemies.Count; i++) {
				enemies [i].Draw (spriteBatch);
			}

			for (int i = 0; i < explosions.Count; i++) {
				explosions [i].Draw (spriteBatch);
			}

			for (int i = 0; i < lasers.Count; i++) {
				lasers [i].Draw (spriteBatch);
			}

			player.Draw (spriteBatch);

			spriteBatch.Draw (healthBar, healthBarRec, Color.White);
			spriteBatch.DrawString (font, "Score: " + score, new Vector2 (600, 10), Color.White);
		}
	}
}

