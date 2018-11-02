// A component that implements the list of given transactions as a table
Vue.component("transactions", {
    props: ["network", "source", "method", "noindex"],
	template:
	`<div class="table-responsive">
        <table class="table table-sm table-striped border-bottom border-primary table_info_trans">
            <thead class="thead-light">
                <tr>
                    <th v-show="noindex === undefined">№</th>
                    <th>Id</th>                                
                    <th>From account</th>
                    <th>To account</th>
                    <th>Value</th>
                    <th>Fee</th>
                </tr>
            </thead>
            <tbody>
                <tr v-for="item in source">
                    <td v-show="noindex === undefined">{{item.index}}</td>                
                    <td class="hash"><a :href="network + '/transaction/' + item.id">{{item.id}}</a></td>               
                    <td class="hash"><a :href="network + '/account/' + item.fromAccount">{{item.fromAccount}}</a></td>
                    <td class="hash"><a :href="network + '/account/' + item.toAccount">{{item.toAccount}}</a></td>
                    <td>{{item.value}} {{item.currency}}</td>
                    <td>{{item.fee}}</td>
                </tr>
            </tbody>
        </table>
    </div>`
});

// Pager component, used for page switching on tables
Vue.component("pb", {
    props: ["page", "getfn", "next", "last", "hidelast"],
    template:
        `<ul class="pagination pagination-sm justify-content-end  my-1" v-show="page > 1 || next">
            <li v-bind:class="{'page-item':true, disabled:page<=1}">
                <a class="page-link" href="#" v-on:click="getfn(1)" >First</a>
            </li>
            <li v-bind:class="{'page-item':true, disabled:page<=1}">
                <a class="page-link" href="#" v-on:click="getfn(page - 1)">Prev</a>
            </li>
            <li class="page-item" v-show="page>1">
                <a class="page-link" href="#" v-on:click="getfn(page - 1)"> {{page-1}} </a>                
            </li>
            <li class="page-item active">
                <a class="page-link" href="#"> {{page}} </a>
            </li>
            <li class="page-item" v-show="(last !== undefined)&&(page+1 <= last)">
                <a class="page-link" href="#" v-on:click="getfn(page + 1)"> {{page+1}} </a>                
            </li>
            <li v-bind:class="{'page-item':true, disabled:!next}">
                <a class="page-link" href="#" v-on:click="getfn(page + 1)">Next</a>
            </li>
            <li v-bind:class="{'page-item':true, disabled:last === undefined || page >= last}" >
                <a class="page-link" href="#" v-on:click="getfn(last)">Last</a>
            </li>
        </ul>`
});

