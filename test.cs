using System;
using CommunityToolkit.Mvvm.Input;
public class Test {
    public void Run() {
        var cmd = new RelayCommand(Execute);
    }
    private void Execute() {}
}
