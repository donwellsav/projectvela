using System.ComponentModel;
using ConferencePlayer.Utils;
using Xunit;

namespace ConferencePlayer.App.Tests.Utils;

public class ObservableObjectTests
{
    private class TestObservableObject : ObservableObject
    {
        private string? _name;
        public string? Name
        {
            get => _name;
            set => Set(ref _name, value);
        }

        private int _age;
        public int Age
        {
            get => _age;
            set => Set(ref _age, value);
        }

        // Expose Set explicitly for testing return values
        public bool UpdateName(string? newName)
        {
            return Set(ref _name, newName, nameof(Name));
        }

        public void RaisePropertyChangedExplicitly(string propertyName)
        {
            Raise(propertyName);
        }
    }

    [Fact]
    public void Set_UpdatesField_WhenValueChanges()
    {
        // Arrange
        var obj = new TestObservableObject();
        var expectedName = "New Name";

        // Act
        obj.Name = expectedName;

        // Assert
        Assert.Equal(expectedName, obj.Name);
    }

    [Fact]
    public void Set_ReturnsTrue_WhenValueChanges()
    {
        // Arrange
        var obj = new TestObservableObject();
        var expectedName = "Changed Name";

        // Act
        bool result = obj.UpdateName(expectedName);

        // Assert
        Assert.True(result);
        Assert.Equal(expectedName, obj.Name);
    }

    [Fact]
    public void Set_RaisesPropertyChanged_WhenValueChanges()
    {
        // Arrange
        var obj = new TestObservableObject();
        string? changedProp = null;
        obj.PropertyChanged += (s, e) => changedProp = e.PropertyName;

        // Act
        obj.Name = "Test";

        // Assert
        Assert.Equal(nameof(obj.Name), changedProp);
    }

    [Fact]
    public void Set_ReturnsFalse_WhenValueUnchanged()
    {
        // Arrange
        var obj = new TestObservableObject();
        obj.Name = "Initial"; // Set initial value

        // Act
        bool result = obj.UpdateName("Initial");

        // Assert
        Assert.False(result);
        Assert.Equal("Initial", obj.Name);
    }

    [Fact]
    public void Set_DoesNotRaisePropertyChanged_WhenValueUnchanged()
    {
        // Arrange
        var obj = new TestObservableObject();
        obj.Name = "Initial";
        bool eventRaised = false;
        obj.PropertyChanged += (s, e) => eventRaised = true;

        // Act
        obj.Name = "Initial";

        // Assert
        Assert.False(eventRaised);
    }

    [Fact]
    public void Raise_FiresPropertyChanged_Explicitly()
    {
        // Arrange
        var obj = new TestObservableObject();
        string? changedProp = null;
        obj.PropertyChanged += (s, e) => changedProp = e.PropertyName;
        string customProp = "CustomProp";

        // Act
        obj.RaisePropertyChangedExplicitly(customProp);

        // Assert
        Assert.Equal(customProp, changedProp);
    }

    [Fact]
    public void Set_HandlesNullValues_Correctly()
    {
        // Arrange
        var obj = new TestObservableObject();
        obj.Name = "NotNull";
        string? changedProp = null;
        obj.PropertyChanged += (s, e) => changedProp = e.PropertyName;

        // Act - Set to null
        bool setResult = obj.UpdateName(null);

        // Assert
        Assert.True(setResult);
        Assert.Null(obj.Name);
        Assert.Equal(nameof(obj.Name), changedProp);

        // Act - Set to null again (no change)
        changedProp = null;
        bool secondResult = obj.UpdateName(null);

        // Assert
        Assert.False(secondResult);
        Assert.Null(changedProp);
    }
}
