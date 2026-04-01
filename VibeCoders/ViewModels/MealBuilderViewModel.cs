using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VibeCoders.Models;

namespace VibeCoders.ViewModels
{
    /// <summary>
    /// ViewModel for the MealBuilderForm UserControl.
    /// Self-contained: manages name, ingredient tags, and instructions.
    /// Raises <see cref="MealSaved"/> when the trainer confirms a meal,
    /// then resets the form automatically.
    /// </summary>
    public partial class MealBuilderViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _mealName = string.Empty;

        [ObservableProperty]
        private string _currentIngredientInput = string.Empty;

        [ObservableProperty]
        private string _instructions = string.Empty;

        /// <summary>Live tag-chip list bound to the ingredient repeater.</summary>
        public ObservableCollection<IngredientItem> Ingredients { get; } = new();

        // ── Commands ─────────────────────────────────────────────────────────

        [RelayCommand]
        private void AddIngredient()
        {
            string trimmed = CurrentIngredientInput.Trim();
            if (string.IsNullOrEmpty(trimmed)) return;
            if (Ingredients.Any(i => i.Text.Equals(trimmed, StringComparison.OrdinalIgnoreCase))) return;

            Ingredients.Add(new IngredientItem(trimmed, RemoveIngredient));
            CurrentIngredientInput = string.Empty;
        }

        private void RemoveIngredient(IngredientItem item) => Ingredients.Remove(item);

        [RelayCommand]
        private void SaveMeal()
        {
            if (string.IsNullOrWhiteSpace(MealName)) return;

            var meal = new Meal
            {
                Name         = MealName.Trim(),
                Ingredients  = Ingredients.Select(i => i.Text).ToList(),
                Instructions = Instructions.Trim(),
            };

            MealSaved?.Invoke(this, meal);
            Reset();
        }

        /// <summary>
        /// Raised after <see cref="SaveMealCommand"/> succeeds.
        /// Subscribers (e.g. TrainerDashboardView) can collect the built meal
        /// and add it to the current <see cref="NutritionPlan"/>.
        /// </summary>
        public event EventHandler<Meal>? MealSaved;

        private void Reset()
        {
            MealName                = string.Empty;
            CurrentIngredientInput  = string.Empty;
            Instructions            = string.Empty;
            Ingredients.Clear();
        }
    }

    /// <summary>
    /// A single ingredient tag in the dynamic list.
    /// Holds its own <see cref="RemoveCommand"/> so the DataTemplate
    /// can bind directly without any RelativeSource gymnastics.
    /// </summary>
    public partial class IngredientItem : ObservableObject
    {
        private readonly Action<IngredientItem> _remove;

        public string Text { get; }

        public IRelayCommand RemoveCommand { get; }

        public IngredientItem(string text, Action<IngredientItem> remove)
        {
            Text          = text;
            _remove       = remove;
            RemoveCommand = new RelayCommand(() => _remove(this));
        }
    }
}
