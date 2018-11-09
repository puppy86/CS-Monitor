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
                    <th>Time</th>
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
                    <td>{{formatDateTime(item.time)}}</td>                    
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

Vue.mixin({
    methods: {
        pad: function (num) {
            var s = `0${num}`;
            return s.substr(s.length - 2);
        },
        getAge: function (time) {
            var daysDiffInMillSec = new Date(this.model.lastBlockData.now) - new Date(time);
            if (daysDiffInMillSec < 0) return "0";
            var daysLeft = Math.floor(daysDiffInMillSec / 86400000);
            daysDiffInMillSec -= daysLeft * 86400000;
            var hoursLeft = Math.floor(daysDiffInMillSec / 3600000);
            daysDiffInMillSec -= hoursLeft * 3600000;
            var minutesLeft = Math.floor(daysDiffInMillSec / 60000);
            daysDiffInMillSec -= minutesLeft * 60000;
            var secLeft = Math.floor(daysDiffInMillSec / 1000);
            var res = daysLeft !== 0 ? daysLeft + "d " : "";
            res += hoursLeft !== 0 || daysLeft !== 0 ? this.pad(hoursLeft) + "h " : "";
            res += this.pad(minutesLeft) + "m " + this.pad(secLeft) + "s";
            return res;
        },
        formatDate: function(time) {
            var dt = new Date(time);
            return `${dt.getFullYear()}/${dt.getMonth()}/${dt.getDate()}`;
        },
        formatTime: function (time) {
            var dt = new Date(time);
            return `${this.pad(dt.getHours())}:${this.pad(dt.getMinutes())}:${this.pad(dt.getSeconds())}`;
        },
        formatDateTime: function (time) {
            return new Date(time).toLocaleString();
        }
    }
});
