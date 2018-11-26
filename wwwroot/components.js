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

var pagesData = {
    limit: localStorage.limit ? localStorage.limit : 25
};

// Pager component, used for page switching on tables
Vue.component("pb", {
    props: {
        page: Number,
        getfn: Function,
        next: Boolean,
        last: Number,
        top: Boolean
    },
    data: function () {
        return pagesData;
    },
    methods: {
        setLimit: function (value) {
            this.limit = value;
            localStorage.limit = value;
            this.getfn(this.page);
        }
    },
    template:
        `<div class="d-sm-flex justify-content-between">
            <div v-show="top" class="my-1"><slot></slot></div>
            <div class="nav-item dropdown my-1" v-show="!top">
             Show
             <div class="btn-group">
                 <button type="button" class="btn btn-outline-secondary btn-sm dropdown-toggle" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false">
                     {{limit}}
                 </button>
                 <div class="dropdown-menu">
                     <a class="dropdown-item" href="#" v-on:click="setLimit(25)">25</a>
                     <a class="dropdown-item" href="#" v-on:click="setLimit(50)">50</a>
                     <a class="dropdown-item" href="#" v-on:click="setLimit(100)">100</a>
                 </div>
             </div>
            </div>
            <ul class="pagination pagination-sm my-1">
                <li v-bind:class="{'page-item':true, disabled:page<=1}">
                    <a class="page-link" href="#" v-on:click="getfn(1)" >First</a>
                </li>
                <li v-bind:class="{'page-item':true, disabled:page<=1}">
                    <a class="page-link" href="#" v-on:click="getfn(page - 1)">Prev</a>
                </li>

                <li class="page-item" v-show="page!=null">
                    <a class="page-link"> {{page}} of {{last}} </a>                
                </li>

                <li v-bind:class="{'page-item':true, disabled:!next}">
                    <a class="page-link" href="#" v-on:click="getfn(page + 1)">Next</a>
                </li>
                <li v-bind:class="{'page-item':true, disabled:last === undefined || page >= last}" >
                    <a class="page-link" href="#" v-on:click="getfn(last)">Last</a>
                </li>
            </ul>
        </div>`
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
        getTimeSpan: function (time) {
            if (time < 0) return "0";
            let sec = time;
            var daysLeft = Math.floor(sec / 86400);
            sec -= daysLeft * 86400;
            var hoursLeft = Math.floor(sec / 3600);
            sec -= hoursLeft * 3600;
            var minutesLeft = Math.floor(sec / 60);
            sec -= minutesLeft * 60;
            
            var res = daysLeft !== 0 ? daysLeft + "d " : "";
            res += hoursLeft !== 0 || daysLeft !== 0 ? this.pad(hoursLeft) + "h " : "";
            res += this.pad(minutesLeft) + "m " + this.pad(sec) + "s";
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
