Vue.component("pools", {
	props: ["network", "source"],
	template:
	`<div class="container_table">
			<table class="table_info_trans">
				<thead>
				<tr>
				    <th>№</th>
					<th>Age</th>
					<th>Status</th>
					<th>Hash</th>				    
					<th>Quant txs</th>
					<th>All value</th>
					<th>All fee</th>
				</tr>
				</thead>
				<tbody>
				<tr v-for="item in source">
				    <td>{{item.number}}</td>
					<td>{{item.age}}</td>
					<td v-bind:class="{success: item.status, nosuccess: !item.status}">{{item.status ? 'Success' :'No success'}}</td>
					<td class="hash"><a :href="network + '/monitor/ledger/' + item.hash">{{item.hash}}</a></td>				    
					<td>{{item.txCount}}</td>
					<td>{{item.value}}</td>
					<td>{{item.fee}}</td>
				</tr>
				</tbody>
			</table>
		</div>`
});

Vue.component("transactions", {
    props: ["network", "source"],
	template:
	`<div class="container_table">
        <table class="table_info_trans">
            <thead>
            <tr>
                <th>№</th>
                <th>Id</th>                                
                <th>From account</th>
                <th>To account</th>
                <th>Value</th>
                <th>Fee</th>
            </tr>
            </thead>
            <tbody>
            <tr v-for="item in source">
                <td>{{item.index}}</td>                
                <td class="hash"><a :href="network + '/monitor/transaction/' + item.id">{{item.id}}</a></td>               
                <td class="hash"><a :href="network + '/monitor/account/' + item.fromAccount">{{item.fromAccount}}</a></td>
                <td class="hash"><a :href="network + '/monitor/account/' + item.toAccount">{{item.toAccount}}</a></td>
                <td>{{item.value}} {{item.currency}}</td>
                <td>{{item.fee}}</td>
            </tr>
            </tbody>
        </table>
    </div>`
});

Vue.component("pager", {
	props: ["page", "getfn", "next", "last"],
	template:
		`<div class="pagination">
			<button v-on:click="getfn(1)" v-bind:disabled="page<=1">First</button>
			<button v-on:click="getfn(page - 1)" v-bind:disabled="page<=1">Prev</button>
		    <div class='page'>
		        <div class='select'>Page {{page}}</div>	        
		    </div>	
			<button v-on:click="getfn(page + 1)" v-bind:disabled="!next">Next</button>
			<button v-bind:disabled="last === undefined || page == last" v-on:click="getfn(last)">Last</button>
		</div>`
});
