# infinibattle-2019 - Planet Wars

## Notes from after the fact:  
Started out with just calculating some random facts, without creating any strategy.  
Started early with the idea to do it all perfectly.... but, oh you know, life...  

### Distances and GameState recreation
Calculated all shortest paths in `Models/GameState.cs`. Planets don't move and the calculation is quite heavy, so altered `Bot.cs` to adjust the `GameState` instead of creating a new one each turn. Only `Planet`'s `Owner` and `Health` properties are overwritten, but the `GameState.Ships` is still overwritten entirely.  

### Tests
Though straying far from the code initially imagined, added some small tests with injected `GameState`, to fix the most obvious bugs and get the strategy and calculations running without uploading it yet.  
Later also captured/wrote/adjusted entire game-init and turn input, using `Console.SetIn` to test the entire bot, which helped a lot with finding and fixing the more subtle (not exception-throwing but sending ships with -4000 health 🙄) bugs. 

### Additional notes...
Added `TODO`'s with idea's and things to sort out or adjust throughout the time, but sadly enough a lot of them have stayed.

The actual strategy lives in `Turn.cs`.

`Health` of `Planet`s is calculated not a fixed amount of turns ahead, but enough turns to reach neighbouring `Planet`s or inbound `Ship`s

😰
