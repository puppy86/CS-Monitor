
Vue.component("transactions", {
    props: ["network", "source"],
	template:
	`<div class="container_table tabler mb-20">
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
	props: ["page", "getfn", "next", "last", "hidelast"],
	template:
	    `<div class="pagination">
			<button class="page_control" v-on:click="getfn(1)" v-bind:disabled="page<=1">First</button>
			<button class="page_control" v-on:click="getfn(page - 1)" v-bind:disabled="page<=1">Prev</button>
	        <span class="page_select">Page {{page}}</span>
			<button class="page_control" v-on:click="getfn(page + 1)" v-bind:disabled="!next">Next</button>
			<button class="page_control" v-show="hidelast === undefined" v-bind:disabled="last === undefined || page >= last" v-on:click="getfn(last)">Last</button>
		</div>`
});
