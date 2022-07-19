using System.Collections.Generic;
using System.Linq;

namespace Furball.Vixie; 

/// <summary>
/// Manages GameComponents, so that when they get added they get initialized and when removed unloaded
/// </summary>
public class GameComponentCollection : GameComponent {
    /// <summary>
    /// List of all GameComponents
    /// </summary>
    private List<GameComponent> _components;
    /// <summary>
    /// Creates a GameComponentCollection
    /// </summary>
    public GameComponentCollection() {
        this._components = new List<GameComponent>();
    }
    /// <summary>
    /// Adds a GameComponent to the Component list and Initializes it
    /// </summary>
    /// <param name="component">Component to Add</param>
    public void Add(GameComponent component) {
        this._components.Add(component);

        component.Initialize();
        //We need to make sure the Component list is sorted so we process it in the right order in Update an Draw
        this._components = this._components.OrderByDescending(c => c.ProcessOrder).ToList();
    }
    /// <summary>
    /// Removes a GameComponent from the list and unloads & disposes it
    /// </summary>
    /// <param name="component">Component to Remove</param>
    public void Remove(GameComponent component) {
        this._components.Remove(component);

        component.Unload();
        component.Dispose();
    }
    /// <summary>
    /// Draws all added GameComponents
    /// </summary>
    /// <param name="deltaTime">Time since last Draw</param>
    public override void Draw(double deltaTime) {
        for (int i = 0; i != this._components.Count; i++) {
            GameComponent current = this._components[i];

            current.Draw(deltaTime);
        }
    }
    /// <summary>
    /// Updates all added GameComponents
    /// </summary>
    /// <param name="deltaTime">Time since last Update</param>
    public override void Update(double deltaTime) {
        for (int i = 0; i != this._components.Count; i++) {
            GameComponent current = this._components[i];

            current.Update(deltaTime);
        }
    }
    /// <summary>
    /// Disposes all GameComponents
    /// </summary>
    public override void Dispose() {
        for (int i = 0; i != this._components.Count; i++) {
            GameComponent current = this._components[i];

            current.Dispose();
        }
    }
}