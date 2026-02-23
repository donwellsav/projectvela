using System;
using System.Windows.Input;
using ConferencePlayer.Utils;
using Xunit;

namespace ConferencePlayer.App.Tests.Utils;

public class RelayCommandTests
{
    // Tests for the non-generic RelayCommand
    [Fact]
    public void Constructor_NullExecute_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new RelayCommand(null!));
    }

    [Fact]
    public void CanExecute_NoPredicate_ReturnsTrue()
    {
        var command = new RelayCommand(() => { });
        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public void CanExecute_WithPredicate_ReturnsCorrectValue()
    {
        var commandTrue = new RelayCommand(() => { }, () => true);
        Assert.True(commandTrue.CanExecute(null));

        var commandFalse = new RelayCommand(() => { }, () => false);
        Assert.False(commandFalse.CanExecute(null));
    }

    [Fact]
    public void Execute_InvokesAction()
    {
        bool executed = false;
        var command = new RelayCommand(() => executed = true);
        command.Execute(null);
        Assert.True(executed);
    }

    [Fact]
    public void RaiseCanExecuteChanged_RaisesEvent()
    {
        var command = new RelayCommand(() => { });
        bool eventRaised = false;
        command.CanExecuteChanged += (s, e) => eventRaised = true;
        command.RaiseCanExecuteChanged();
        Assert.True(eventRaised);
    }

    // Tests for the generic RelayCommand<T>
    [Fact]
    public void GenericConstructor_NullExecute_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new RelayCommand<string>(null!));
    }

    [Fact]
    public void GenericCanExecute_NoPredicate_ReturnsTrue()
    {
        var command = new RelayCommand<string>(s => { });
        Assert.True(command.CanExecute("test"));
    }

    [Fact]
    public void GenericCanExecute_WithPredicate_ReturnsCorrectValue()
    {
        var commandTrue = new RelayCommand<string>(s => { }, s => s == "test");
        Assert.True(commandTrue.CanExecute("test"));
        Assert.False(commandTrue.CanExecute("other"));
    }

    [Fact]
    public void GenericExecute_InvokesAction_WithParameter()
    {
        string? received = null;
        var command = new RelayCommand<string>(s => received = s);
        command.Execute("test");
        Assert.Equal("test", received);
    }

    [Fact]
    public void GenericExecute_WithNullParameter_InvokesAction()
    {
        bool executed = false;
        string? received = "initial";
        var command = new RelayCommand<string>(s =>
        {
            executed = true;
            received = s;
        });
        command.Execute(null);
        Assert.True(executed);
        Assert.Null(received);
    }

    [Fact]
    public void GenericExecute_WithInvalidType_ThrowsInvalidCastException()
    {
        var command = new RelayCommand<string>(s => { });
        Assert.Throws<InvalidCastException>(() => command.Execute(123)); // Passing int to string command
    }
}
