for /f %%n in (packages.list) do (
	nuget push %%n -s http://nuget.vitaliy.org/ rVdwLNymxSXzXBaaFKtDSGFFsL4nFu4dJmGtc3gG6F5BNVPFRD2P5HESQZjJDQFp
)